

class IlluminateException(Exception):
    def __init__(self, message:str, inner:Exception|None = None):
        super().__init__(message)
        self._message = message
        self._inner = inner

    @property
    def message(self):
        return self._message

    @property
    def inner(self):
        return self._inner