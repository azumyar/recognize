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


from typing import Any, Callable, Iterable, Optional, NamedTuple, Literal
import src.val as val

__print_val = print
def print(
    *values:object,
    sep:str | None = " ",
    end:str | None = "\n",
    #file: SupportsWrite[str] | None = None,
    file:Any | None = None,
    flush:Literal[False] = False) -> None:
    """
    cp932に安全に変換してprintする
    """

    __print_val(
        str(*values).encode("cp932", errors="ignore").decode('cp932'),
        sep=sep,
        end=end,
        file=file,
        flush=flush)

class Enviroment:
    """
    実行環境クラス
    """
    @staticmethod
    def init_system():
        import sys
        is_verbose = False
        verbose = val.ARG_DEFAULT_VERBOSE
        for arg in sys.argv:
            if is_verbose:
                is_verbose = False
                verbose = arg
                break
            if arg == val.ARG_NAME_VERBOSE:
                is_verbose = True
                continue
        return Enviroment(int(verbose))


    def __init__(self, verbose:int) -> None:
        import sys
        import os
        self.__verbose = verbose
        self.__is_exe = sys.argv[0].endswith(".exe")
        if self.__is_exe:
            proj_root = os.path.dirname(os.path.abspath(sys.argv[0]))
            self.__root = proj_root
        else:
            proj_root = f"{os.path.dirname(os.path.abspath(__file__))}{os.sep}.."
            self.__root = f"{proj_root}{os.sep}.debug"

    @property
    def is_exe(self):
        """
        exeコンテナで実行されている場合True
        """
        return self.__is_exe

    @property
    def verbose(self) -> int:
        """
        verbose
        """
        return self.__verbose

    @property
    def root(self) -> str:
        """
        スクリプト実行環境の作業用rootディレクトリ
        """
        return self.__root


class Logger:
    @staticmethod
    def init_system(verbose:int, defualt_log_dir:str):
        import sys
        is_log_dir = False
        is_log_file = False
        log_dir = defualt_log_dir
        log_file = val.ARG_DEFAULT_LOG_FILE
        for arg in sys.argv:
            if is_log_dir:
                is_log_dir = False
                log_dir = arg
                break
            if is_log_file:
                is_log_file = False
                log_file = arg
                break

            if arg == val.ARG_NAME_LOG_DIRECTORY:
                is_log_dir = True
                continue
            if arg == val.ARG_NAME_LOG_FILE:
                is_log_file = True
                continue
        return Logger(verbose, log_dir, log_file)

    def __init__(self, verbose:int, dir:str, file:str) -> None:
        import io
        import os
        self.__verbose = verbose
        
        self.__file_io:io.TextIOWrapper|None = None
        try:
            self.__file_io = open(
                f"{dir}{os.sep}{file}",
                 "w",
                encoding="UTF-8",
                newline="")
        except OSError as e:
            print("##########################")
            print("ログファイルを開けません")
            print(e)
            print("##########################")


    @property
    def is_min(self) -> bool: return val.VERBOSE_MIN <= self.__verbose
    @property
    def is_info(self) -> bool: return val.VERBOSE_INFO <= self.__verbose
    @property
    def is_debug(self) -> bool: return val.VERBOSE_DEBUG <= self.__verbose
    @property
    def is_trace(self) -> bool: return val.VERBOSE_TRACE <= self.__verbose


    def __print(
            self,
            obj:Any,
            is_print:bool,
            sep:str|None = " ",
            end:str|None = "\n",
            ) -> None:
        if obj and is_print:
            print(str(obj), sep=sep, end=end)

    def print(self, obj:Any, sep:str|None = " ", end:str|None = "\n") -> None: self.__print(obj, True, sep=sep, end=end)
    def info(self, obj:Any, sep:str|None = " ", end:str|None = "\n") -> None: self.__print(obj, self.is_min, sep=sep, end=end)
    def notice(self, obj:Any, sep:str|None = " ", end:str|None = "\n") -> None: self.__print(obj, self.is_info, sep=sep, end=end)
    def debug(self, obj:Any, sep:str|None = " ", end:str|None = "\n") -> None: self.__print(obj, self.is_debug, sep=sep, end=end)
    def trace(self, obj:Any, sep:str|None = " ", end:str|None = "\n") -> None: self.__print(obj, self.is_trace, sep=sep, end=end)

    def log(self, arg:object) -> None:
        import os
        import datetime
        if self.__file_io is None:
            return

        time = datetime.datetime.now()
        s:str
        if isinstance(arg, Iterable):
            s = f"{os.linesep}".join(map(lambda x: f"{x}", arg))
        else:
            s = f"{arg}"
        self.__file_io.write(f"{time}{os.linesep}{s}{os.linesep}{os.linesep}")
        self.__file_io.flush()


ilm_enviroment:Enviroment = Enviroment.init_system()
ilm_logger:Logger = Logger.init_system(ilm_enviroment.verbose, ilm_enviroment.root)