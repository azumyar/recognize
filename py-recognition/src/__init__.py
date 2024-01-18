# https://qiita.com/maebaru/items/f5fecf752c4cf9321a48
# IPv6だと遅い…？
# 一度保留 
#import socket
#
#def getAddrInfoWrapper(host, port, family=0, socktype=0, proto=0, flags=0):
#    # IPv4に限定する
#    return origGetAddrInfo(host, port, socket.AF_INET, socktype, proto, flags)
#
#origGetAddrInfo = socket.getaddrinfo
#socket.getaddrinfo = getAddrInfoWrapper

import src.google_recognizers as google

#google.recognize_google = google.recognize_google_urllib
#google.recognize_google_duplex = google.recognize_google_duplex_urllib
google.recognize_google = google.recognize_google_requests
google.recognize_google_duplex = google.recognize_google_duplex_requests
