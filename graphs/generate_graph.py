import pandas as pd
import matplotlib
import matplotlib.pyplot as plt
import argparse
import os
import numpy as np
from matplotlib.ticker import MaxNLocator

# TODO: THIS IS NOT NEEDED I CAN REMOVE THIS ....

parser = argparse.ArgumentParser(
    description="Saves a graph of the link flooding attack simulation over multiple frames."
)

parser.add_argument("in_path", help="Path of the csv file to read from.")
parser.add_argument(
    "out_path", help="Path of the output file. Includes the name of the file."
)

args = parser.parse_args()
# Load data from CSV file
path = args.in_path
if not os.path.isfile(path):
    raise Exception("This path does not exist.")
data = pd.read_csv(path)

matplotlib.rcParams["font.sans-serif"] = "Times New Roman"
matplotlib.rcParams["font.size"] = 13
plt.rcParams["figure.figsize"] = (10, 6) #(14, 8)

fig, ax1 = plt.subplots()

link_area = list()  # list of start and end frame pairs (where a link has been found.)


def isnan(object):
    return not isinstance(object, str) and np.isnan(object) 

# Plotting
prevLink = np.nan
start_empty = data["FRAME"].iloc[0]
end_empty = data["FRAME"].iloc[-1]
empty_area = None
currentLink = np.nan # ""
for index, row in data.iterrows():
    currentLink = row["TARGET LINK"]
    if isnan(currentLink) and isnan(prevLink):
        continue

    if currentLink != prevLink:
        if isnan(currentLink):
            plt.axvline(x=row["FRAME"], color="grey", linestyle="--", linewidth=0.8)
            start_empty = data["FRAME"].iloc[index]
            link_area.append((end_empty, start_empty))

        elif isnan(prevLink):
            end_empty = data["FRAME"].iloc[index]
            empty_area = plt.axvspan(
                start_empty,
                end_empty,
                color="red",
                alpha=0.2,
                label="No target link found",
            )

        else:
            plt.axvline(x=row["FRAME"], color="grey", linestyle="--", linewidth=0.8)

        prevLink = currentLink

if not isnan(currentLink):
    link_area.append((end_empty, data["FRAME"].iloc[-1]))
else:
    empty_area = plt.axvspan(
                start_empty,
                data["FRAME"].iloc[-1],
                color="red",
                alpha=0.2,
                label="No target link found",)
    

# Attack route plot
color = "tab:orange"
textcolor = "black"
ax1.set_xlabel("Number of Snapshots")
ax1.yaxis.set_major_locator(MaxNLocator(integer=True))
ax1.set_ylabel("Number of Attack Routes", color=textcolor)
line1 = None
for start_area, end_area in link_area:
    (line1,) = ax1.plot(
        data["FRAME"][start_area:end_area],
        data["ROUTE COUNT"][start_area:end_area],
        color=color,
        marker=".",
        label="Number of attack routes",
    )
ax1.tick_params(axis="y", labelcolor=textcolor)

# Final capacity plot
ax2 = ax1.twinx()
color = "tab:cyan"
ax2.set_ylabel("Final Capacity of the Target Link", color=textcolor)
line2 = None
for start_area, end_area in link_area:
    (line2,) = ax2.plot(
        data["FRAME"][start_area:end_area],
        data["FINAL CAPACITY"][start_area:end_area],
        color=color,
        marker=".",
        label="Final capacity of the target link",
    )
ax2.tick_params(axis="y", labelcolor=textcolor)
ax2.set_ylim(-500, 20000)

lines = []
if line1 is not None:
    lines.append(line1)
if line2 is not None:
    lines.append(line2)
if empty_area is not None:
    lines.append(empty_area)
labels = [line.get_label() for line in lines]

plt.subplots_adjust(top=0.8)
plt.subplots_adjust(bottom=0.1)
plt.subplots_adjust(right=.85)
plt.subplots_adjust(left=.08)
plt.legend(lines, labels, bbox_to_anchor=(0, 1.02, 1, 0.2), loc="lower left", mode="expand", ncol=len(lines)) 

# Title and grid
# plt.title('Number of Attack Routes and Final Capacity of the Target Link over the Frame Count')
plt.grid(True)

# Show the plot
output = args.out_path
if not output.endswith(".svg"):
    raise Exception("This file needs to have a .svg extension.")
# plt.show()
plt.savefig(output)
