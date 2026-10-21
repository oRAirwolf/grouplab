import ObjectiveC
import XCTest

/// NOTES-FROM-PLANNING.md entry 353 step 2: real taps on the simulator for scripts/touch-test.py.
///
/// The folder named by GROUPLAB_TAPS (TEST_RUNNER_GROUPLAB_TAPS to xcodebuild) is the conversation: this writes "ready" once it can tap;
/// the script writes "request", "x y seconds" in screen points; this presses there for that long and writes "done"; "stop" ends it. The
/// press goes through the springboard, whose frame is the whole screen, so it lands on whatever is there, GroupLab Dev or the keyboard,
/// exactly as a finger would, and XCTest never waits on GroupLab Dev, which draws continuously, to fall idle.
///
/// Entry 461 section 2.1: a request of "audit" runs XCTest's own accessibility audit on GroupLab Dev as it shows, writes every issue to
/// "audit-result.json" (its type, its short and long description, and the element's label and frame where it has one) and then "done";
/// scripts/screen-reader-audit.py judges them.
final class TouchTests: XCTestCase {
    func testTaps() throws {
        guard let folder = ProcessInfo.processInfo.environment["GROUPLAB_TAPS"], !folder.isEmpty else {
            XCTFail("GROUPLAB_TAPS names no folder")
            return
        }

        let files = FileManager.default
        let request = (folder as NSString).appendingPathComponent("request")
        let done = (folder as NSString).appendingPathComponent("done")
        let stop = (folder as NSString).appendingPathComponent("stop")
        NoWaitForIdle.apply()
        let springboard = XCUIApplication(bundleIdentifier: "com.apple.springboard")
        let corner = springboard.coordinate(withNormalizedOffset: CGVector(dx: 0, dy: 0))
        try "ready".write(toFile: (folder as NSString).appendingPathComponent("ready"), atomically: true, encoding: .utf8)

        let end = Date().addingTimeInterval(30 * 60)
        while Date() < end {
            if files.fileExists(atPath: stop) {
                return
            }

            if let text = try? String(contentsOfFile: request, encoding: .utf8) {
                try? files.removeItem(atPath: request)
                if text.hasPrefix("audit") {
                    let issues = Audit.run(XCUIApplication(bundleIdentifier: "org.grouplab.app.dev"))
                    let data = try JSONSerialization.data(withJSONObject: issues, options: [.prettyPrinted])
                    try data.write(to: URL(fileURLWithPath: (folder as NSString).appendingPathComponent("audit-result.json")), options: .atomic)
                    try "audit".write(toFile: done, atomically: true, encoding: .utf8)
                    continue
                }

                let numbers = text.split(whereSeparator: { $0 == " " || $0 == "\n" }).compactMap { Double(String($0)) }
                if numbers.count >= 2 {
                    corner.withOffset(CGVector(dx: numbers[0], dy: numbers[1])).press(forDuration: numbers.count > 2 ? numbers[2] : 0.15)
                    try "\(numbers[0]) \(numbers[1])".write(toFile: done, atomically: true, encoding: .utf8)
                }
            }

            Thread.sleep(forTimeInterval: 0.1)
        }

        XCTFail("nothing said stop in 30 minutes")
    }
}

/// Entry 461 section 2.1: XCTest's accessibility audit (Xcode 15 and later), every type, each issue kept and none failing the test here.
enum Audit {
    static func run(_ app: XCUIApplication) -> [[String: Any]] {
        var issues: [[String: Any]] = []
        do {
            try app.performAccessibilityAudit(for: .all) { issue in
                var row: [String: Any] = ["type": name(issue.auditType), "summary": issue.compactDescription, "detail": issue.detailedDescription]
                if let element = issue.element {
                    row["label"] = element.label
                    let frame = element.frame
                    row["frame"] = [frame.minX, frame.minY, frame.width, frame.height]
                }
                issues.append(row)
                return true
            }
        } catch {
            issues.append(["type": "error", "summary": "the audit did not run: \(error)"])
        }
        return issues
    }

    static func name(_ type: XCUIAccessibilityAuditType) -> String {
        let names: [(XCUIAccessibilityAuditType, String)] = [
            (.contrast, "contrast"), (.elementDetection, "elementDetection"), (.hitRegion, "hitRegion"),
            (.sufficientElementDescription, "sufficientElementDescription"), (.dynamicType, "dynamicType"), (.textClipped, "textClipped"),
            (.trait, "trait"),
        ]
        return names.first { type.contains($0.0) }?.1 ?? "other \(type.rawValue)"
    }
}

/// Entry 447 section 3: Real taps run 39243232233 went red because XCTest spent 34 seconds in "Wait for com.apple.springboard to idle"
/// before one press, with GroupLab Dev answering throughout, and the press that followed lost its release. A finger does not wait for the
/// home screen to settle, so neither does this: XCTest's wait for an application to idle is replaced with one that returns at once. The
/// method is XCTest's own and not public, so each name it has had is tried and every one replaced is printed in touch-runner.log.
enum NoWaitForIdle {
    static func apply() {
        guard let process = NSClassFromString("XCUIApplicationProcess") else {
            print("NoWaitForIdle: XCUIApplicationProcess not found; XCTest still waits for idle")
            return
        }

        let one: @convention(block) (AnyObject, Bool) -> Void = { _, _ in }
        let two: @convention(block) (AnyObject, Bool, Bool) -> Void = { _, _, _ in }
        let names: [(String, AnyObject)] = [
            ("waitForQuiescenceIncludingAnimationsIdle:", unsafeBitCast(one, to: AnyObject.self)),
            ("waitForQuiescenceIncludingAnimationsIdle:isPreEvent:", unsafeBitCast(two, to: AnyObject.self)),
        ]
        var replaced = 0
        for (name, block) in names {
            if let method = class_getInstanceMethod(process, NSSelectorFromString(name)) {
                method_setImplementation(method, imp_implementationWithBlock(block))
                print("NoWaitForIdle: replaced \(name)")
                replaced += 1
            }
        }

        if replaced == 0 {
            print("NoWaitForIdle: no wait for idle found; XCTest still waits for idle")
        }
    }
}
