from argparse import ArgumentParser
import json

# Experiments command: python3 ./write_experiments_file.py experiments_final.json -f 30 -r 100

# Parse the arguments.
parser = ArgumentParser()
parser.add_argument("filename")
parser.add_argument("-f", "--frames", type=int, required=True)
parser.add_argument("-r", "--reps", type=int, required=True)
args = parser.parse_args()


# YOU MAY MODIFY THE BELOW IF YOU'D LIKE.
imax_list = {1, 3, 6, 9}
scenarios = {
    #  1 : {0, 1},            # Landlocked
     2 : {0, 1, 2, 3},      # Coastal
     # exclude Insular
     4 : {0, 1},            # Polar
     5 : {0, 1, 2, 3},      # Equatorial
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


# Add the desired experiments.
for scenario, directions in scenarios.items():
    for direction in directions:
        for imax in imax_list:
            data["experiments"].append({
                "choice": scenario,
                "direction": direction,
                "rMax": imax,
                "reps": args.reps
            })


# Write to a JSON file.s
json_data = json.dumps(data, indent=4)
with open(args.filename, "w") as file:
     file.write(json_data)