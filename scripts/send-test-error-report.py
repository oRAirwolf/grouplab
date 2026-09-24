#!/usr/bin/env python3
"""Send one made-up error report to grouplab.org, NOTES-FROM-PLANNING.md entry 194 section 5.3.

    python scripts/send-test-error-report.py

It checks the whole path once the server part is installed: the receiver takes it, the worker turns it into an
issue in oRAirwolf/grouplab-crash-reports within five minutes. The report is plainly a test: its error is
"TestReport", thrown nowhere, and its last action says so. Nothing about this computer is in it.

A good result is one line, "sent: the receiver took it", and then an issue titled
"TestReport in ErrorReportCheck.Send" in the private repository, labeled "survived".
"""

from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request

RECEIVER = "https://grouplab.org/api/error-report.php"


def main() -> int:
    report = {
        "schema": "grouplab-error-report-1",
        "report_id": os.urandom(16).hex(),
        "kind": "survived",
        "made": "automatic",
        "count": 1,
        "app": {"version": "0.2.0-nightly.0", "commit": "test", "channel": "test"},
        "environment": {"os": "a test report, not a computer", "framework": "", "renderer": "", "display_scale": 1},
        "exceptions": [{"type": "GroupLab.Test.TestReport", "message": "A test report sent to check the path from the application to the issue.",
                        "stack": "   at GroupLab.Test.ErrorReportCheck.Send()"}],
        "last_actions": ["test.report"],
    }
    data = urllib.parse.urlencode({"report": json.dumps(report)}).encode("utf-8")
    request = urllib.request.Request(RECEIVER, data=data, method="POST", headers={"User-Agent": "GroupLab test report"})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            answer = json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        print(f"not sent: the receiver answered {e.code}, {e.read().decode('utf-8', 'replace')[:200]}")
        return 1
    except (urllib.error.URLError, TimeoutError) as e:
        print(f"not sent: the receiver could not be reached ({type(e).__name__})")
        return 1
    if answer.get("ok"):
        print("sent: the receiver took it")
        return 0
    print(f"not sent: {answer.get('error', answer)}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
