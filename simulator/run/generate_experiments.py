"""Script to generate the specification for experiments."""

import os
from typing import Any
from argparse import ArgumentParser
import json
from datetime import date


def list_of_ints(arg: Any) -> list:
    return list(map(int, arg.split(",")))


# Parse the arguments.
parser = ArgumentParser()
parser.add_argument("-f", "--frames", type=int, required=True)
parser.add_argument("-r", "--reps", type=int, required=True)
parser.add_argument("-n", "--name", type=str, required=True)
parser.add_argument(
    "-v",
    "--vulnerable",
    action="store_true",
    default=False,
    help="Runs LFAs on vulnerable scenarios.",
)
parser.add_argument(
    "--imaxes",
    type=list_of_ints,
    help="List of positive non-nil integers which are separated only by a comma. Defaults to i_max == OFF.",
    default=[1],
)
parser.add_argument("--log_frames", action="store_true", default=False)
parser.add_argument("--log_video", action="store_true", default=False)
parser.add_argument("--log_attack", action="store_true", default=False)
parser.add_argument("--log_rtt", action="store_true", default=False)
parser.add_argument("--log_hops", action="store_true", default=False)
args = parser.parse_args()


# constants.
DIRECTIONS = [0, 1, 2, 3]
ATTRIBUTES = [1, 2, 3, 4, 5, 6, 7]
VULNERABLE_ATTRIBUTE2DIRECTIONS = {
    1: [0, 1, 2, 3],  # Landlocked
    2: [0, 1, 2, 3],  # Coastal
    4: [0, 1],        # Polar
    5: [0, 1, 2, 3],  # Equatorial
    7: [0, 1, 2, 3],  # Intraorbital
}


# template.
data = {
    "experiments": list(),
    "frames": args.frames,
    "logScreenshots": args.log_frames,
    "logVideo": args.log_video,
    "logAttack": args.log_attack,
    "logRTT": args.log_rtt,
    "logHops": args.log_hops,
}


# add experiments.
if not args.vulnerable:
    for attribute in ATTRIBUTES:
        for direction in DIRECTIONS:
            for imax in args.imaxes:
                data["experiments"].append(
                    {
                        "choice": 0,
                        "direction": direction,
                        "rMax": imax,
                        "reps": args.reps,
                    }
                )
else:
    for attribute, directions in VULNERABLE_ATTRIBUTE2DIRECTIONS.items():
        for direction in directions:
            for imax in args.imaxes:
                data["experiments"].append(
                    {
                        "choice": attribute,
                        "direction": direction,
                        "rMax": imax,
                        "reps": args.reps,
                    }
                )


# write to file.
json_data = json.dumps(data, indent=4)
if not os.path.exists("experiments"):
    os.makedirs("experiments")
filename = "experiments/%iframes_%ireps_%s.json" % (args.frames, args.reps, args.name)
with open(filename, "w") as file:
    file.write(json_data)
    print("Wrote specification to file: %s" % (file.name))
