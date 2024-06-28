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

import os
def is_prod_or_debug() -> bool:
    '''
    exe化の際に__init.py__が実行されるので判定する
    '''
    import sys
    return sys.argv[0] == "-m" or sys.argv[0].endswith(".exe")

def _root_path() -> str:
    import sys
    __is_exe = sys.argv[0].endswith(".exe")
    if __is_exe:
        proj_root = os.path.dirname(os.path.abspath(sys.argv[0]))
        __root = proj_root
        return __root
    else:
        proj_root = f"{os.path.dirname(os.path.abspath(__file__))}{os.sep}.."
        __root = f"{proj_root}{os.sep}.debug"
        if is_prod_or_debug():
            os.makedirs(__root, exist_ok=True)
            os.chdir(__root)
        return __root
# kotoba-whisperのダウンロード設定をする
os.environ["HUGGINGFACE_HUB_CACHE"] = f"{_root_path()}{os.sep}.cache"

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
        self.__root = _root_path()

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
        log_rotate = False
        for arg in sys.argv:
            if is_log_dir:
                is_log_dir = False
                log_dir = arg
                continue
            if is_log_file:
                is_log_file = False
                log_file = arg
                continue

            if arg == val.ARG_NAME_LOG_DIRECTORY:
                is_log_dir = True
                continue
            if arg == val.ARG_NAME_LOG_FILE:
                is_log_file = True
                continue
            if arg == val.ARG_NAME_LOG_ROTATE:
                log_rotate = True
                continue
        return Logger(
            verbose,
            log_dir,
            log_file, 
            log_rotate)

    def __init__(self, verbose:int, dir:str, file:str, rotate:bool) -> None:
        import io
        import os
        import re
        import datetime
        self.__verbose = verbose
        
        self.__file_io:io.TextIOWrapper|None = None
        try:
            if is_prod_or_debug():
                f = file
                if rotate:
                    regexp = re.compile("^(.+)(\\..+)$", re.RegexFlag.IGNORECASE)
                    m = regexp.match(file)
                    d = datetime.datetime.now().strftime('%Y-%m-%d_%H-%M-%S')
                    if not m is None:
                        f = f"{m.group(1)}.{d}{m.group(2)}"
                    else:
                        f = f"{f}.{d}"

                self.__file_io = open(
                    f"{dir}{os.sep}{f}",
                    "w",
                    encoding="UTF-8",
                    newline="")
        except OSError as e:
            print(f"{val.Console.Red.value}##########################")
            print(f"{val.Console.Red.value}ログファイルを開けません")
            print(f"{val.Console.Red.value}{e}")
            print(f"{val.Console.Red.value}##########################{val.Console.Reset.value}")


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
            out_file:bool|None = None,
            console:val.Console|list[val.Console]|str|None = None,
            reset_console:bool=False,
            ) -> None:
        con:str = ""
        if isinstance(console, val.Console):
            con = console.value
        if isinstance(console, list):
            con = "".join(map(lambda x: str(x.value), console))
        elif isinstance(console, str):
            con = console
        if reset_console:
            r_con = val.Console.Reset.value
        else:
            r_con = ""

        if obj and is_print:
            print(f"{con}{obj}{r_con}", sep=sep, end=end)
        if not out_file is None and out_file:
            self.log(obj)

    def print(self, obj:Any, sep:str|None=" ", end:str|None="\n", console:val.Console|str|None=None, reset_console:bool=False, out_file:bool|None=None) -> None: self.__print(obj, True, sep=sep, end=end, console=console, reset_console=reset_console, out_file=out_file)
    def info(self, obj:Any, sep:str|None=" ", end:str|None="\n", console:val.Console|str|None=val.Console.Cyan, reset_console:bool=False, out_file:bool|None=None) -> None: self.__print(obj, self.is_min, sep=sep, end=end, console=console, reset_console=reset_console, out_file=out_file)
    def notice(self, obj:Any, sep:str|None=" ", end:str|None="\n", console:val.Console|str|None=None, reset_console:bool=False, out_file:bool|None=None) -> None: self.__print(obj, self.is_info, sep=sep, end=end, console=console, reset_console=reset_console, out_file=out_file)
    def debug(self, obj:Any, sep:str|None=" ", end:str|None="\n", console:val.Console|list[val.Console]|str|None=[val.Console.Yellow, val.Console.BackgroundBlack], reset_console:bool=False, out_file:bool|None=None) -> None: self.__print(obj, self.is_debug, sep=sep, end=end, console=console, reset_console=reset_console, out_file=out_file)
    def trace(self, obj:Any, sep:str|None=" ", end:str|None="\n", console:val.Console|list[val.Console]|str|None=[val.Console.Yellow, val.Console.BackgroundBlack, val.Console.UnderLine], reset_console:bool=False, out_file:bool|None=None) -> None: self.__print(obj, self.is_trace, sep=sep, end=end, console=console, reset_console=reset_console, out_file=out_file)

    def error(self, obj:Any, sep:str|None = " ", end:str|None = "\n", console:val.Console|str|None=None) -> None:
        '''
        標準出力とファイル両方にだす verbose >= min
        '''
        _console = console
        if _console is None:
            _console = val.Console.Red
        self.info(self.__join(obj), sep, end, out_file=True, console=_console, reset_console=True)

    def log(self, arg:object) -> None:
        '''
        ログファイルに出力
        '''
        import os
        import datetime
        if self.__file_io is None:
            return

        time = datetime.datetime.now()
        self.__file_io.write(f"{time}{os.linesep}{self.__join(arg)}{os.linesep}{os.linesep}")
        self.__file_io.flush()
    
    def __join(self, obj:object) -> object:
        '''
        objがイテレータの場合結合して文字とする
        '''
        import os
        if isinstance(obj, Iterable) and not isinstance(obj, str):
            return os.linesep.join(map(lambda x: str(x), obj))
        else:
            return obj

def rms2db(rms:float, p0:float=1.) -> float:
    '''
    RMS値をdB値にする
    '''
    import math
    return 20 * math.log10(max(rms, 1)/ p0)

def db2rms(db:float, p0:float=1.) -> float:
    '''
    dB値をRMS値にする
    '''
    return (10 ** (db / 20)) * p0

ilm_enviroment:Enviroment = Enviroment.init_system()
ilm_logger:Logger = Logger.init_system(ilm_enviroment.verbose, ilm_enviroment.root)

