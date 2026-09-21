#!/usr/bin/env python3
import json
import sys
import urllib.error
import urllib.request

import w15_runtime_acceptance as original


def request(method: str, path: str, *, bearer=None, org=None, body=None, idem=None):
    url = f"{original.BASE_URL}{path}"
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
            if not raw:
                parsed = None
            else:
                content_type = str(response.headers.get("Content-Type", "")).lower()
                if "json" in content_type:
                    parsed = json.loads(raw)
                else:
                    parsed = raw
            return response.status, response.headers, parsed
    except urllib.error.HTTPError as error:
        raw = error.read().decode()
        try:
            parsed = json.loads(raw) if raw else None
        except json.JSONDecodeError:
            parsed = raw
        return error.code, error.headers, parsed


def openapi_gate():
    swagger_status, _, _ = request("GET", "/swagger")
    if swagger_status != 200:
        original.fail(f"Swagger expected 200, got {swagger_status}")

    _, _, doc = original.expect_status(
        "OpenAPI",
        request("GET", "/swagger/v1/swagger.json"),
        200,
    )
    if not isinstance(doc, dict):
        original.fail("OpenAPI expected JSON object")

    paths = doc.get("paths", {})
    collection = paths.get("/api/v1/terminals", {})
    resource = paths.get("/api/v1/terminals/{terminalId}", {})
    expected = [
        (collection, "get", "listTerminals"),
        (collection, "post", "registerTerminal"),
        (resource, "get", "getTerminal"),
        (resource, "patch", "updateTerminal"),
    ]

    for node, method, operation_id in expected:
        actual = node.get(method, {}).get("operationId")
        if actual != operation_id:
            original.fail(
                f"OpenAPI {method} expected operationId {operation_id}, got {actual}"
            )

    if "delete" in collection or "delete" in resource:
        original.fail("OpenAPI unexpectedly exposes DELETE for terminals")

    print("PASS OpenAPI terminal surface: 4/4 exact operations, no DELETE")


_configured_companies = set()
_original_create_location = original.create_location


def ensure_company_profile(org: str, bearer: str):
    if org in _configured_companies:
        return

    status, _, body = request("GET", "/api/v1/company", bearer=bearer, org=org)
    if status == 200:
        if not isinstance(body, dict) or body.get("organizationId") != org:
            original.fail(f"existing company profile for {org}: unexpected projection {body}")
        _configured_companies.add(org)
        print(f"PASS company prerequisite already configured for {org}")
        return

    if (
        status != 404
        or not isinstance(body, dict)
        or body.get("code") != "organization.company_not_configured"
    ):
        original.fail(
            f"company prerequisite lookup for {org}: expected 200 or company_not_configured 404, "
            f"got HTTP {status}, body={body}"
        )

    suffix = "01" if org == original.ORG_A else "02"
    digits = "".join(ch for ch in original.STAMP if ch.isdigit())
    ruc = f"{digits[-10:].rjust(10, '0')}{suffix}"
    profile_request = {
        "ruc": ruc,
        "legalName": f"W1.5 QA Issuer {suffix}",
        "commercialName": f"W15 QA {suffix}",
    }
    _, _, profile = original.expect_status(
        f"configure company prerequisite {org}",
        request(
            "PATCH",
            "/api/v1/company",
            bearer=bearer,
            org=org,
            body=profile_request,
            idem=f"w15-{original.STAMP}-company-{suffix}",
        ),
        200,
    )
    if (
        not isinstance(profile, dict)
        or profile.get("organizationId") != org
        or profile.get("ruc") != ruc
        or profile.get("version") != 1
    ):
        original.fail(f"configure company prerequisite {org}: unexpected projection {profile}")

    _configured_companies.add(org)


def create_location(org, bearer, name, branch_code, key):
    ensure_company_profile(org, bearer)
    return _original_create_location(org, bearer, name, branch_code, key)


original.request = request
original.openapi_gate = openapi_gate
original.create_location = create_location


if __name__ == "__main__":
    try:
        original.main()
    except Exception as exc:
        print(f"W1.5 RUNTIME ACCEPTANCE: FAIL: {exc}", file=sys.stderr)
        raise
