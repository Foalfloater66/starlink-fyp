from ._stat import Stat


class ExpectedMaliciousTraffic(Stat):

    def _get_data(
        self, scenario: str, direction: str, imax_list: List[int], reps: int
    ) -> pd.DataFrame:
        aggregate_data = []
        # print(reps)
        for (
            direction
        ) in directions:  # TODO: DIRECTIONS IS IMPORTANT TOO> NEED TO USE THIS.
            for imax in [3, 6, 9]:  # kwargs["imaxes"]:
                # print(imax)
                imax = f"{imax}" if imax != 1 else "OFF"
                agg_data = []
                # Concatenate data from different iterations into a single DataFrame.
                for i in range(0, reps):  # ran 100 instances.
                    filename = f"{scenario}_{direction}_{
                        imax}_{i:03d}/attack.csv"
                    # assert(os.path.isfile(filename))
                    i_data = load_csv_data(filename).explode(
                        "FINAL CAPACITY", ignore_index=True
                    )
                    # i_data['FINAL CAPACITY'] = i_data['FINAL CAPACITY'].apply(lambda x: abs(x - 20000)) # TODO: AND MAKE SURE ITS NOT IN MBITS BUT IN GBITS PER SEC
                    i_data["direction"] = f"{scenario} {direction}"
                    i_data["imax"] = imax
                    agg_data.append(i_data)
                new = pd.concat(agg_data, axis=0, ignore_index=True)
                aggregate_data.append(new)

        return pd.concat(aggregate_data, axis=0, ignore_index=True)

    def _barplot(self, data_df: pd.DataFrame):
        sns.displot(
            data=data_df,
            x="imax",
            y="FINAL CAPACITY",
            legend=True,
            # palette=palette,
            hue="direction",
            ci="sd",
            capsize=0.2,
            estimator=np.mean,
        )

    def _plot(
        self, scenario: str, direction: str, imax_list: List[str], reps: int, **kwargs
    ):
        assert reps >= 0
        data_df = self._get_data(scenario, direction, imax_list, reps)
        self._barplot(data_df)

    def single_plot(
        self, scenario: str, direction: str, imax_list: int, reps: int, **kwargs
    ):
        self._plot(scenario, direction, imax_list, reps, **kwargs)
        plt.tight_layout()
        plt.legend()
        plt.show()

    def multi_plot(self):
        raise NotImplementedError
