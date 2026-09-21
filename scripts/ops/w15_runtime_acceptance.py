#!/usr/bin/env python3
import base64
import hashlib
import hmac
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

BASE_URL = os.environ["BASE_URL"].rstrip("/")
JWT_KEY = os.environ["JWT_KEY"]
JWT_ISSUER = os.environ["JWT_ISSUER"]
JWT_AUDIENCE = os.environ["JWT_AUDIENCE"]
STAMP = os.environ.get("W15_STAMP", str(int(time.time())))
ORG_A = f"w15-qa-{STAMP}-a"
ORG_B = f"w15-qa-{STAMP}-b"


def b64(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode()


def token(sub: str, permissions: str, company_scope: str) -> str:
    now = int(time.time())
    header = {"alg": "HS256", "typ": "JWT"}
    payload = {
        "sub": sub,
        "iss": JWT_ISSUER,
        "aud": JWT_AUDIENCE,
        "iat": now,
        "nbf": now - 5,
        "exp": now + 1200,
        "permission": permissions,
        "company_scope": company_scope,
    }
    encoded_header = b64(json.dumps(header, separators=(",", ":")).encode())
    encoded_payload = b64(json.dumps(payload, separators=(",", ":")).encode())
    unsigned = f"{encoded_header}.{encoded_payload}"
    signature = hmac.new(JWT_KEY.encode(), unsigned.encode(), hashlib.sha256).digest()
    return f"{unsigned}.{b64(signature)}"


FULL_PERMISSIONS = ",".join([
    "organization.read",
    "organization.manage",
    "security.roles.read",
    "security.users.read",
    "parties.read",
    "catalog.read",
    "sales.read",
    "fiscal.read",
])
TOKEN_A = token("w15-runtime-actor-a", FULL_PERMISSIONS, ORG_A)
TOKEN_B = token("w15-runtime-actor-b", FULL_PERMISSIONS, ORG_B)
TOKEN_A_READ = token("w15-runtime-read-a", "organization.read", ORG_A)
TOKEN_A_NO_ORG = token("w15-runtime-no-org-permission-a", "parties.read", ORG_A)


def fail(message: str):
    raise AssertionError(message)


def request(method: str, path: str, *, bearer=None, org=None, body=None, idem=None):
    url = f"{BASE_URL}{path}"
    data = None
    headers = {"Accept": "application/json"}
    if bearer:
        headers["Authorization"] = f"Bearer {bearer}"
    if org:
        headers["X-Organization-Id"] = org
    if idem:
        headers["Idempotency-Key"] = idem
    if body is not None:
        data = json.dumps(body, separators=(",", ":")).encode()
        headers["Content-Type"] = "application/json"
    req = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            raw = response.read().decode()
            parsed = json.loads(raw) if raw else None
            return response.status, response.headers, parsed
    except urllib.error.HTTPError as error:
        raw = error.read().decode()
        try:
            parsed = json.loads(raw) if raw else None
        except json.JSONDecodeError:
            parsed = raw
        return error.code, error.headers, parsed


def expect_status(label, result, expected):
    status, _, body = result
    if status != expected:
        fail(f"{label}: expected HTTP {expected}, got {status}, body={body}")
    print(f"PASS {label}: HTTP {status}")
    return result


def expect_problem(label, result, status, code, conflict_type=None):
    _, _, body = expect_status(label, result, status)
    if not isinstance(body, dict):
        fail(f"{label}: expected Problem Details JSON object")
    if body.get("code") != code:
        fail(f"{label}: expected code {code}, got {body.get('code')}")
    if conflict_type is not None and body.get("conflictType") != conflict_type:
        fail(f"{label}: expected conflictType {conflict_type}, got {body.get('conflictType')}")
    return body


def auth_headers_replay(headers):
    return str(headers.get("Idempotent-Replayed", "")).lower() == "true"


def location_body(name, branch_code, active=None, expected_version=None):
    body = {
        "name": name,
        "dgiBranchCode": branch_code,
        "fiscalAddress": f"QA {name}",
        "city": "Montevideo",
        "department": "Montevideo",
    }
    if active is not None:
        body["active"] = active
    if expected_version is not None:
        body["expectedVersion"] = expected_version
    return body


def create_location(org, bearer, name, branch_code, key):
    status, headers, body = expect_status(
        f"create location {name}",
        request("POST", "/api/v1/locations", bearer=bearer, org=org,
                body=location_body(name, branch_code), idem=key),
        201,
    )
    if body.get("organizationId") != org or body.get("active") is not True or body.get("version") != 1:
        fail(f"create location {name}: unexpected projection {body}")
    return body


def update_location(org, bearer, current, active, expected_version, key):
    body = location_body(
        current["name"], current["dgiBranchCode"], active=active, expected_version=expected_version
    )
    _, _, updated = expect_status(
        f"update location {current['name']} active={active}",
        request("PATCH", f"/api/v1/locations/{current['id']}", bearer=bearer, org=org,
                body=body, idem=key),
        200,
    )
    return updated


def openapi_gate():
    swagger_status, _, _ = request("GET", "/swagger")
    if swagger_status != 200:
        fail(f"Swagger expected 200, got {swagger_status}")
    status, _, doc = expect_status("OpenAPI", request("GET", "/swagger/v1/swagger.json"), 200)
    paths = doc.get("paths", {})
    collection = paths.get("/api/v1/terminals", {})
    resource = paths.get("/api/v1/terminals/{terminalId}", {})
    expected = {
        (collection, "get"): "listTerminals",
        (collection, "post"): "registerTerminal",
        (resource, "get"): "getTerminal",
        (resource, "patch"): "updateTerminal",
    }
    for (node, method), operation_id in expected.items():
        actual = node.get(method, {}).get("operationId")
        if actual != operation_id:
            fail(f"OpenAPI {method} expected operationId {operation_id}, got {actual}")
    if "delete" in collection or "delete" in resource:
        fail("OpenAPI unexpectedly exposes DELETE for terminals")
    print("PASS OpenAPI terminal surface: 4/4 exact operations, no DELETE")


def main():
    openapi_gate()

    expect_status("terminals without JWT", request("GET", "/api/v1/terminals", org=ORG_A), 401)
    expect_status("terminals malformed JWT", request("GET", "/api/v1/terminals", bearer="invalid", org=ORG_A), 401)
    expect_status("terminals missing organization.read", request("GET", "/api/v1/terminals", bearer=TOKEN_A_NO_ORG, org=ORG_A), 403)
    expect_status(
        "terminal POST with read-only permission",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A_READ, org=ORG_A,
                body={"code": "DENIED", "name": "Denied", "locationId": "missing"}, idem=f"w15-{STAMP}-denied"),
        403,
    )
    expect_problem(
        "organization escape",
        request("GET", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_B),
        403,
        "organization_scope_denied",
    )

    loc_a1 = create_location(ORG_A, TOKEN_A, "W15 QA Location A1", "0001", f"w15-{STAMP}-loc-a1-create")
    loc_a2 = create_location(ORG_A, TOKEN_A, "W15 QA Location A2", "0002", f"w15-{STAMP}-loc-a2-create")
    loc_b1 = create_location(ORG_B, TOKEN_B, "W15 QA Location B1", "9001", f"w15-{STAMP}-loc-b1-create")

    code_b_raw = f"  w15.{STAMP}.b  "
    code_b = f"W15.{STAMP}.B"
    t1_body = {"code": code_b_raw, "name": "  W1.5 Terminal B  ", "locationId": loc_a1["id"]}
    t1_key = f"w15-{STAMP}-t1-register"
    _, _, t1 = expect_status(
        "register T1",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A, body=t1_body, idem=t1_key),
        201,
    )
    if t1.get("organizationId") != ORG_A or t1.get("code") != code_b or t1.get("name") != "W1.5 Terminal B" or t1.get("locationId") != loc_a1["id"] or t1.get("active") is not True or t1.get("version") != 1:
        fail(f"register T1 canonical projection mismatch: {t1}")

    status, headers, replay = expect_status(
        "register T1 deterministic replay",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A, body=t1_body, idem=t1_key),
        201,
    )
    if not auth_headers_replay(headers) or replay.get("id") != t1["id"] or replay.get("version") != 1:
        fail("register T1 replay did not preserve resource/version or replay header")

    expect_problem(
        "register idempotency payload mismatch",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A,
                body={**t1_body, "name": "Changed under same key"}, idem=t1_key),
        409,
        "idempotency_key_reused",
        "payload_mismatch",
    )
    expect_problem(
        "duplicate normalized terminal code",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A,
                body={"code": code_b.lower(), "name": "Duplicate", "locationId": loc_a1["id"]}, idem=f"w15-{STAMP}-duplicate"),
        409,
        "organization.terminal.code_duplicate",
        "duplicate_terminal_code",
    )
    expect_problem(
        "cross-organization location on register",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A,
                body={"code": f"W15.{STAMP}.X", "name": "Cross org", "locationId": loc_b1["id"]}, idem=f"w15-{STAMP}-cross-location"),
        404,
        "organization.location_not_found",
    )
    expect_problem(
        "missing location on register",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A,
                body={"code": f"W15.{STAMP}.M", "name": "Missing", "locationId": "missing-location"}, idem=f"w15-{STAMP}-missing-location"),
        404,
        "organization.location_not_found",
    )

    code_a = f"W15.{STAMP}.A"
    _, _, t2 = expect_status(
        "register T2",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A,
                body={"code": code_a, "name": "W1.5 Terminal A", "locationId": loc_a1["id"]}, idem=f"w15-{STAMP}-t2-register"),
        201,
    )
    if t2.get("active") is not True or t2.get("version") != 1:
        fail(f"register T2 lifecycle mismatch: {t2}")

    _, _, active_list = expect_status("list active terminals", request("GET", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A), 200)
    ids = [row.get("id") for row in active_list]
    expected_ids = [t2["id"], t1["id"]]
    if ids != expected_ids:
        fail(f"deterministic list order mismatch: expected {expected_ids}, got {ids}")

    query = urllib.parse.urlencode({"locationId": loc_a1["id"]})
    _, _, location_list = expect_status("list by location", request("GET", f"/api/v1/terminals?{query}", bearer=TOKEN_A, org=ORG_A), 200)
    if [x["id"] for x in location_list] != expected_ids:
        fail("location-filtered terminal list mismatch")

    _, _, got_t1 = expect_status("get T1", request("GET", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A), 200)
    if got_t1.get("id") != t1["id"]:
        fail("get T1 returned wrong resource")
    expect_problem(
        "cross-organization terminal get",
        request("GET", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_B, org=ORG_B),
        404,
        "organization.terminal_not_found",
    )

    loc_a2 = update_location(ORG_A, TOKEN_A, loc_a2, False, 1, f"w15-{STAMP}-loc-a2-off")
    if loc_a2.get("version") != 2 or loc_a2.get("active") is not False:
        fail("A2 did not deactivate to version 2")
    expect_problem(
        "register onto inactive location",
        request("POST", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A,
                body={"code": f"W15.{STAMP}.I", "name": "Inactive target", "locationId": loc_a2["id"]}, idem=f"w15-{STAMP}-inactive-register"),
        409,
        "organization.terminal.location_inactive",
        "inactive_location",
    )
    loc_a2 = update_location(ORG_A, TOKEN_A, loc_a2, True, 2, f"w15-{STAMP}-loc-a2-on")
    if loc_a2.get("version") != 3 or loc_a2.get("active") is not True:
        fail("A2 did not reactivate to version 3")

    reassign_body = {"name": "W1.5 Terminal B moved", "locationId": loc_a2["id"], "active": True, "expectedVersion": 1}
    reassign_key = f"w15-{STAMP}-t1-reassign"
    _, _, t1_v2 = expect_status(
        "reassign T1",
        request("PATCH", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A, body=reassign_body, idem=reassign_key),
        200,
    )
    if t1_v2.get("version") != 2 or t1_v2.get("locationId") != loc_a2["id"] or t1_v2.get("active") is not True:
        fail(f"T1 reassignment mismatch: {t1_v2}")

    status, headers, replay = expect_status(
        "reassign T1 deterministic replay",
        request("PATCH", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A, body=reassign_body, idem=reassign_key),
        200,
    )
    if not auth_headers_replay(headers) or replay.get("version") != 2:
        fail("T1 update replay did not preserve version/header")

    expect_problem(
        "update idempotency payload mismatch",
        request("PATCH", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A,
                body={**reassign_body, "name": "Changed under reused update key"}, idem=reassign_key),
        409,
        "idempotency_key_reused",
        "payload_mismatch",
    )
    stale = expect_problem(
        "stale terminal update",
        request("PATCH", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A,
                body={"name": t1_v2["name"], "locationId": loc_a2["id"], "active": True, "expectedVersion": 1}, idem=f"w15-{STAMP}-t1-stale"),
        409,
        "concurrency_conflict",
        "stale_version",
    )
    if str(stale.get("currentVersion")) != "2":
        fail(f"stale update currentVersion expected 2, got {stale.get('currentVersion')}")

    _, _, t1_v3 = expect_status(
        "deactivate T1",
        request("PATCH", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A,
                body={"name": t1_v2["name"], "locationId": loc_a2["id"], "active": False, "expectedVersion": 2}, idem=f"w15-{STAMP}-t1-off"),
        200,
    )
    if t1_v3.get("version") != 3 or t1_v3.get("active") is not False:
        fail("T1 did not deactivate to version 3")

    loc_a2 = update_location(ORG_A, TOKEN_A, loc_a2, False, 3, f"w15-{STAMP}-loc-a2-off-final")
    _, _, t1_v4 = expect_status(
        "rename inactive T1 while location inactive",
        request("PATCH", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A,
                body={"name": "W1.5 Terminal B parked", "locationId": loc_a2["id"], "active": False, "expectedVersion": 3}, idem=f"w15-{STAMP}-t1-rename-inactive"),
        200,
    )
    if t1_v4.get("version") != 4 or t1_v4.get("active") is not False:
        fail("inactive rename should succeed and increment to version 4")
    expect_problem(
        "reactivate T1 on inactive location",
        request("PATCH", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A,
                body={"name": t1_v4["name"], "locationId": loc_a2["id"], "active": True, "expectedVersion": 4}, idem=f"w15-{STAMP}-t1-reactivate-inactive"),
        409,
        "organization.terminal.location_inactive",
        "inactive_location",
    )

    blocked_body = location_body(loc_a1["name"], loc_a1["dgiBranchCode"], active=False, expected_version=1)
    expect_problem(
        "location deactivation blocked by active terminal",
        request("PATCH", f"/api/v1/locations/{loc_a1['id']}", bearer=TOKEN_A, org=ORG_A,
                body=blocked_body, idem=f"w15-{STAMP}-loc-a1-blocked"),
        409,
        "organization.location.active_terminals_exist",
        "active_terminal_dependency",
    )

    _, _, t2_v2 = expect_status(
        "deactivate T2",
        request("PATCH", f"/api/v1/terminals/{t2['id']}", bearer=TOKEN_A, org=ORG_A,
                body={"name": t2["name"], "locationId": loc_a1["id"], "active": False, "expectedVersion": 1}, idem=f"w15-{STAMP}-t2-off"),
        200,
    )
    if t2_v2.get("version") != 2 or t2_v2.get("active") is not False:
        fail("T2 did not deactivate to version 2")
    loc_a1 = update_location(ORG_A, TOKEN_A, loc_a1, False, 1, f"w15-{STAMP}-loc-a1-off-final")
    loc_b1 = update_location(ORG_B, TOKEN_B, loc_b1, False, 1, f"w15-{STAMP}-loc-b1-off-final")

    _, _, default_active = expect_status("final default active list", request("GET", "/api/v1/terminals", bearer=TOKEN_A, org=ORG_A), 200)
    if default_active != []:
        fail(f"final default active list should be empty, got {default_active}")
    _, _, inactive_list = expect_status("final inactive list", request("GET", "/api/v1/terminals?active=false", bearer=TOKEN_A, org=ORG_A), 200)
    if [row.get("id") for row in inactive_list] != expected_ids:
        fail(f"final inactive deterministic order mismatch: {inactive_list}")

    expect_status("terminal DELETE not exposed", request("DELETE", f"/api/v1/terminals/{t1['id']}", bearer=TOKEN_A, org=ORG_A), 405)

    regressions = [
        ("current actor", "/api/v1/me"),
        ("permissions", "/api/v1/permissions"),
        ("roles", "/api/v1/roles"),
        ("users", "/api/v1/users"),
        ("parties", "/api/v1/parties"),
        ("countries", "/api/v1/reference-data/countries"),
        ("uruguay departments", "/api/v1/reference-data/uruguay-departments"),
        ("fiscal identity types", "/api/v1/reference-data/fiscal-identity-types"),
        ("currencies", "/api/v1/reference-data/currencies"),
        ("fiscal document types", "/api/v1/reference-data/fiscal-document-types"),
        ("invoice indicators", "/api/v1/reference-data/invoice-indicators"),
        ("contact types", "/api/v1/reference-data/contact-types"),
        ("units of measure", "/api/v1/reference-data/units-of-measure"),
    ]
    for label, path in regressions:
        expect_status(f"regression {label}", request("GET", path, bearer=TOKEN_A, org=ORG_A), 200)

    summary = {
        "stamp": STAMP,
        "organizationA": ORG_A,
        "organizationB": ORG_B,
        "locationA1": {"id": loc_a1["id"], "version": loc_a1["version"], "active": loc_a1["active"]},
        "locationA2": {"id": loc_a2["id"], "version": loc_a2["version"], "active": loc_a2["active"]},
        "locationB1": {"id": loc_b1["id"], "version": loc_b1["version"], "active": loc_b1["active"]},
        "terminalT1": {"id": t1_v4["id"], "code": t1_v4["code"], "version": t1_v4["version"], "active": t1_v4["active"], "locationId": t1_v4["locationId"]},
        "terminalT2": {"id": t2_v2["id"], "code": t2_v2["code"], "version": t2_v2["version"], "active": t2_v2["active"], "locationId": t2_v2["locationId"]},
        "terminalSuccessfulMutations": 6,
    }
    out = os.environ.get("W15_SUMMARY_PATH")
    if out:
        with open(out, "w", encoding="utf-8") as handle:
            json.dump(summary, handle, indent=2, sort_keys=True)
    print("=================================================")
    print(" W1.5 RUNTIME ACCEPTANCE: PASS")
    print("=================================================")
    print(json.dumps(summary, sort_keys=True))


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"W1.5 RUNTIME ACCEPTANCE: FAIL: {exc}", file=sys.stderr)
        raise
