class CancellationObject:
    """
    非同期実行キャンセル用クラス
    """
    def __init__(self) -> None:
        self.__is_alive = True

    @property
    def alive(self) -> bool:
        """
        Trueなら非同期実行を継続
        """
        return self.__is_alive

    def cancel(self) -> None:
        """
        非同期実行のキャンセルを通知
        """
        self.__is_alive = False
