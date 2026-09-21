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


original.request = request


if __name__ == "__main__":
    try:
        original.main()
    except Exception as exc:
        print(f"W1.5 RUNTIME ACCEPTANCE: FAIL: {exc}", file=sys.stderr)
        raise
