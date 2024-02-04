
from typing import Any, Literal

__print = print 
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

    __print(
        str(*values).encode("cp932", errors="ignore").decode('cp932'),
        sep=sep,
        end=end,
        file=file,
        flush=flush)
