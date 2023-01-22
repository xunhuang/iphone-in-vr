# iphone-in-vr
#
# v0 webtrc to make streaming from browser camera to browser work over websocket (instead of socket-io)
# v1 make streaming from iOS app camera to browser work.
# v2 make iOS streaming from extension to browser work. 
#     v2 - tbd to use H264 encoder to address memory issue. 
#     v2 - tbd to use compiled version instead of debug version. 
#     v2 code run in extension and it won't run in simulator (limited by Apple) so it is pointless not to use Cocoapod... 