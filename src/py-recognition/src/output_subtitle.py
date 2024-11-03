import io
import os
import re
import datetime


class SubtitleOutputer:
    def output(self, text_ja:str, text_en:str) -> None:
        ...

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

    def output(self, text_ja:str, text_en:str) -> None:
        self.__io_ja.seek(0)
        self.__io_en.seek(0)
        self.__io_ja.truncate(0)
        self.__io_en.truncate(0)
        self.__io_ja.write(text_ja)
        self.__io_en.write(text_en)
        self.__io_ja.flush()
        self.__io_en.flush()