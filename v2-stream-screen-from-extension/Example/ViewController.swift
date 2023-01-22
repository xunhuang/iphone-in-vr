//
//  ViewController.swift
//  Example
//
//  Created by Roman on 28.02.2021.
//

import UIKit
import AVFoundation
import AVKit
import ReplayKit

class ViewController: UIViewController {

    var observations: [NSObjectProtocol] = []
    @IBOutlet weak var containerView: UIView!
    private lazy var notificationCenter: NotificationCenter = .default

    override func viewDidLoad() {
        super.viewDidLoad()
        let broadcastPicker = RPSystemBroadcastPickerView(frame: CGRect(x: 0, y: 0, width: 50, height: 50))
        broadcastPicker.preferredExtension = "com.your-app.broadcast.extension"

        containerView.addSubview(broadcastPicker)
    }

    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)



    }

    override func viewDidDisappear(_ animated: Bool) {
        super.viewDidDisappear(animated)

    }
}

