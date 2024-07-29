'''Script to generate the specification for experiments.'''

import os
from typing import Any
from argparse import ArgumentParser
import json
from datetime import date

def list_of_ints(arg: Any) -> list:
    return list(map(int, arg.split(',')))

# Parse the arguments.
parser = ArgumentParser()
parser.add_argument("-f", "--frames", type=int, required=True)
parser.add_argument("-r", "--reps", type=int, required=True)
parser.add_argument("-n", "--name", type=str, required=True)
parser.add_argument("-d", "--test_defence", action="store_true", default=False)
parser.add_argument("--imaxes", type=list_of_ints, help="List of positive non-nil integers which are separated only by a comma. Defaults to i_max == OFF.", default=[1])
parser.add_argument("--log_frames", action="store_true", default=False)
parser.add_argument("--log_video", action="store_true", default=False)
parser.add_argument("--log_attack", action="store_true", default=False)
parser.add_argument("--log_rtt", action="store_true", default=False)
parser.add_argument("--log_hops", action="store_true", default=False)
args = parser.parse_args()

# constants.
IMAXES = args.imaxes
ALL_DIRECTIONS = [0, 1, 2, 3]
SCENARIOS_2_DIRECTIONS_RIDS = {
     1 : [0, 1, 2, 3],      # Landlocked
     2 : [0, 1, 2, 3],      # Coastal
     3 : [],                # Insular
     4 : [0, 1],            # Polar
     5 : [0, 1, 2, 3],      # Equatorial
     6 : [0, 1, 2, 3],      # Intraorbital 
     7 : [0, 1, 2, 3]       # Transorbital
}

# template.
data = {
    "experiments" : list(),
    "frames" : args.frames,
    "logScreenshots": args.log_frames,
    "logVideo": args.log_video,
    "logAttack": args.log_attack,
    "logRTT": args.log_rtt,
    "logHops": args.log_hops,
}

# add experiments.
for scenario, directions in SCENARIOS_2_DIRECTIONS_RIDS.items():
    if not args.test_defence:
        directions = ALL_DIRECTIONS
    for direction in directions:
        for imax in IMAXES:
            data["experiments"].append({
                "choice": scenario,
                "direction": direction,
                "rMax": imax,
                "reps": args.reps
            })

# write to file.
json_data = json.dumps(data, indent=4)
if not os.path.exists("experiments"):
    os.makedirs("experiments")
filename = "experiments/%iframes_%ireps_%s.json" % (args.frames, args.reps, args.name)
with open(filename, "w") as file:
    file.write(json_data)
    print("Wrote specification to file: %s" % (file.name))
