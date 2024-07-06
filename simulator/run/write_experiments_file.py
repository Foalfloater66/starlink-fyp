from argparse import ArgumentParser
import json

# parse the arguments
parser = ArgumentParser()
parser.add_argument("filename")
parser.add_argument("-f", "--frames", type=int, required=True)
parser.add_argument("-r", "--reps", type=int, required=True)

args = parser.parse_args()

reps = args.reps


# YOU MAY MODIFY THIS IF YOU'D LIKE. 
choices = {1, 2, 4, 5, 6, 7} # excludes the Insular.
directions = {0, 1} #, 2, 3}
r_maxes = {1, 3, 6, 9}

choices = {
     1 : {0, 1},            # Landlocked
     2 : {0, 1, 2, 3},      # Coastal
     # exclude Insular
     4 : {0, 1},            # Polar
     5 : {0, 1, 2, 3, 4},    # Equatorial
     6 : {0, 1},            # Intraorbital 
     7 : {0, 1}             # Transorbital
}



data = {
    "experiments" : list(),
    "frames" : args.frames,
    "logScreenshots": False,
    "logAttack": True,
    "logRTT": True,
    "logHops": False
}

# create a dictionary with the appropriate data
for choice in choices:
    for direction in directions:
        for r_max in r_maxes:
            if r_max == 1:
                data["experiments"].append({
                    "choice": choice,
                    "direction": direction,
                    "rMax": r_max,
                    "reps": 1
                })
            else:
                    data["experiments"].append({
                    "choice": choice,
                    "direction": direction,
                    "rMax": r_max,
                    "reps": reps
                })
                    
# write the dictionary to a json file
json_data = json.dumps(data, indent=4)
with open(args.filename, "w") as f:
     f.write(json_data)