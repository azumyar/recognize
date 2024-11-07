import io
import os
import re
import datetime
import time
import threading

class SubtitleOutputer:
    def output(self, text_ja:str, text_en:str) -> None:
        ...

class NopSubtitleOutputer(SubtitleOutputer):
    def output(self, text_ja:str, text_en:str) -> None:
        pass


class FileSubtitleOutputer(SubtitleOutputer):

    def __init__(self, root:str) -> None:
        self.__io_ja = open(
            f"{root}{os.sep}subtitle-ja_JP.txt",
            "w",
            encoding="UTF-8",
            newline="")
        self.__io_en = open(
            f"{root}{os.sep}subtitle-en_US.txt",
            "w",
            encoding="UTF-8",
            newline="")
        self.__prv_time = 0
        #t = threading.Thread(target = self.__scheduler)
        t = threading.Timer(0.1, self.__scheduler)
        t.setDaemon(True)
        t.start()

    def output(self, text_ja:str, text_en:str) -> None:
        self.__io_ja.seek(0)
        self.__io_en.seek(0)
        self.__io_ja.truncate(0)
        self.__io_en.truncate(0)
        self.__io_ja.write(text_ja)
        self.__io_en.write(text_en)
        self.__io_ja.flush()
        self.__io_en.flush()
        self.__prv_time = time.time()

    def __scheduler(self) -> None:
        cur = time.time()
        if 4 < (cur - self.__prv_time):
            self.output("", "")
        t = threading.Timer(0.1, self.__scheduler)
        t.setDaemon(True)
        t.start()
