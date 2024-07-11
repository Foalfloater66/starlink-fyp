from abc import ABC


class Stat(ABC):
    def __init__(self):
        pass

    def plot(self, **kwargs):
        """Draw a plot representing the data."""
        raise NotImplementedError
