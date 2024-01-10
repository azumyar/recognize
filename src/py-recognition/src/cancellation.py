class CancellationObject:
    def __init__(self) -> None:
        self.is_alive = True

    @property
    def alive(self) -> bool:
        return self.is_alive

    def cancel(self) -> None:
        self.is_alive = False
