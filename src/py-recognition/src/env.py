import os
import sys
import time
from typing import Any, Callable, NamedTuple

class PerformanceResult(NamedTuple):
    result:Any
    time:int

class Env:
    """
    実行環境クラス
    """
    def __init__(self, verbose:int) -> None:
        self.__verbose = verbose
        if(sys.argv[0].endswith(".exe")):
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
        return sys.argv[0].endswith(".exe")

    @property
    def verbose(self) -> int:
        """
        verbose
        """
        return self.__verbose

    @property
    def is_debug(self) -> bool:
        """
        ログレベルがDEBUG以上で実行されている場合TRUE
        """
        return 1 <= self.__verbose

    @property
    def is_trace(self) -> bool:
        """
        ログレベルがTRACE以上で実行されている場合TRUE
        """
        return 2 <= self.__verbose

    @property
    def root(self) -> str:
        """
        スクリプト実行環境の作業用rootディレクトリ
        """
        return self.__root
    
    def performance(self, func:Callable[[], Any]) ->  PerformanceResult:
        """
        funcを実行した時間を計測
        """
        start = time.perf_counter() 
        r = func()
        return PerformanceResult(r, int((time.perf_counter()-start) * 1000))
    

    def debug(self, func:Callable[[], Any]) -> Any:
        """
        DEBUG以上で実行されいる場合funcを実行
        """
        if self.is_debug:
            return func()

    def tarce(self, func:Callable[[], Any]) -> Any:
        """
        TRACE以上で実行されいる場合funcを実行
        """
        if self.is_trace:
            return func()

