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


original.request = request
original.openapi_gate = openapi_gate


if __name__ == "__main__":
    try:
        original.main()
    except Exception as exc:
        print(f"W1.5 RUNTIME ACCEPTANCE: FAIL: {exc}", file=sys.stderr)
        raise
