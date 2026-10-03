import SwiftUI

// Entry 353 step 2: the application a UI test must name. It is never opened; the test taps GroupLab Dev through the springboard.
@main
struct TouchHost: App {
    var body: some Scene {
        WindowGroup {
            Text("GroupLab touch runner")
        }
    }
}
