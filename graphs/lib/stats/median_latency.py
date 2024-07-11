from typing import List
import pandas as pd
import seaborn as sns

from utils import load_json_data
from ._stat import Stat


class MedianLatency(Stat):
    def __init__(self):
        self.data_df = pd.DataFrame()
        pass

    def get_data(self, scenario: str, direction: str, imax: int, reps: int):
        # Concatenate data from different iterations into a single DataFrame.
        aggregate_data = []
        for i in range(0, reps):
            filename = f"{scenario}_{direction}_{imax}_{i:03d}/rtt.json"
            data = load_json_data(filename).explode("rtt", ignore_index=True)
            data["direction"] = direction
            aggregate_data.append(data)
        return pd.concat(aggregate_data, axis=0, ignore_index=True)

    def _cdf_plot(self, scenario, direction, data_df):
        sns.ecdfplot(
            data=data_df,
            x="rtt",
            # y="rtt",
            label=(
                f"{scenario} {direction}"
                if data_df["rtt"].notna().any()
                else "__nolegend__"
            ),
            legend=True,
        )

    def _timeseries_plot(self, scenario, direction, data_df):
        # Plot the data
        sns.lineplot(
            data=data_df,
            x="frame",
            y="rtt",
            label=(
                f"{scenario} {direction}"
                if data_df["rtt"].notna().any()
                else "__nolegend__"
            ),
            legend=True,
        )

        pass

    def _plot(self, scenario: str, direction: str, imax: int, reps: int, **kwargs):
        """
        NOTE: plot_type defaults to line
        """
        assert reps >= 0

        data_df = self.get_data(scenario, direction, imax, reps)

        if "plot_type" in kwargs and kwargs["plot_type"] == "cdf":
            self._cdf_plot(scenario, direction, data_df)
        else:
            self._timeseries_plot(scenario, direction, data_df)

    def plot(
        self,
        scenario: str,
        directions: List[str],
        imax_list: List[str],
        reps: int,
        **kwargs,
    ):
        for imax in imax_list:
            for direction in directions:
                self._plot(scenario, direction, imax, reps, **kwargs)
        plt.tight_layout()
        plt.legend()
        plt.show()
