# TODO: I THINK THIS HASNT PROGRESSED MUCH.
from ._stat import Stat


class CongestionProbability(Stat):
    def __init__(self):
        pass

    def get_data(self):
        pass

    def plot(self, scenario: str, direction: str, imax: str):
        total_successes = 0
        total_attempts = 0
        for i in range(0, reps):
            filename = f"{scenario}_{direction}_{imax}_{i:03d}/attack.csv"
            data = load_csv_data(filename)
            if 0.0 in data["FINAL CAPACITY"].value_counts():
                total_successes += data["FINAL CAPACITY"].value_counts()[0.0]
            total_attempts += len(data)
        probability = total_successes/total_attempts
        return probability
        sns.barplot(
            x=f"{qualitative_case}",  # {direction}",
            y=probability,
            # color=palette[idx],
            # ax=ax,

            label=f"{qualitative_case} {direction}",
            legend=True
        )
        # return True
        # pass

    def single_plot(self):

        pass

    def multi_plot(self):
        pass


def plot_probability_congestion(qualitative_case, direction, imax, reps, **kwargs) -> float:
    total_successes = 0
    total_attempts = 0
    for i in range(0, reps):
        filename = f"{qualitative_case}_{direction}_{imax}_{i:03d}/attack.csv"
        data = load_csv_data(filename)
        if 0.0 in data["FINAL CAPACITY"].value_counts():
            total_successes += data["FINAL CAPACITY"].value_counts()[0.0]
        total_attempts += len(data)
    probability = total_successes/total_attempts
    return probability
    sns.barplot(
        x=f"{qualitative_case}",  # {direction}",
        y=probability,
        # color=palette[idx],
        # ax=ax,

        label=f"{qualitative_case} {direction}",
        legend=True
    )
    return True


fig, axs = plt.subplots(2, 2, figsize=(10, 10))

for ax in axs:
    # ax.set_xlabel("Snapshot")
    ax.set_ylabel("Probability of Congestion")
    # ax.set_ylim(-.5, 20.5)  # TODO: CHANGE THIS.
    # ax.tick_params(axis="y", labelcolor="black")
    ax.grid(True)

    # Remove the subplot if there is no data.
    if len(ax.get_lines()) == 0:
        fig.delaxes(ax)

plt.tight_layout()
plt.legend()
plt.show()
