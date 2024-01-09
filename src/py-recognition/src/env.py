import os
import sys
import time
import ctypes

class Env:
    def __init__(self, verbose:int) -> None:
        self._verbose = verbose
        if(sys.argv[0].endswith(".exe")):
            proj_root = os.path.dirname(os.path.abspath(sys.argv[0]))
            self._root = proj_root
        else:
            proj_root = f"{os.path.dirname(os.path.abspath(__file__))}{os.sep}.."
            self._root = f"{proj_root}{os.sep}.debug"

    @property
    def is_exe(self):
        return sys.argv[0].endswith(".exe")

    @property
    def is_debug(self) -> bool:
        return 1 <= self._verbose

    @property
    def is_trace(self) -> bool:
        return 2 <= self._verbose

    @property
    def root(self):
        return self._root
    
    def performance(self, func:lambda:any) ->  tuple[any, int]:
        start = time.perf_counter() 
        r = func()
        return (r, int((time.perf_counter()-start) * 1000))
    

    def debug(self, func:lambda:any) -> any:
        if self.is_debug:
            return func()

    def tarce(self, func:lambda:any) -> any:
        if self.is_trace:
            return func()

