# color palette reference: https://www.codecademy.com/article/seaborn-design-ii

__SCENARIOS = {
    "Landlocked": {
        # "palette":
        "type": "Geographical",
        "directions": ["East", "West"]
    },
    "Coastal": {
        # "palette":
        "type": "Geographical",
        "directions": ["East", "West"]
    },
    "Polar": {
        # "palette":
        "type": "Latitudinal",
        "directions": ["East", "West", "North", "South"]
    },
    "Equatorial": {
        # "palette":
        "type": "Latitudinal",
        "directions": ["East", "West", "North", "South"]
    },
    "IntraOrbital": {
        # "palette":
        "type": "Orbital",
        "directions": ["East", "West"]
    },
    "TransOrbital": {
        # "palette":
        "type": "Orbital",
        "directions": ["East", "West"]
    }
}


class ScenarioAttribute(dict):
    def __init__(self, attr: str):
        self.attr = attr

    def __getitem__(self, key: str):
        return __SCENARIOS[key][self.attr]


LOG_DIRECTORY = "../../simulator/Logs/"
DIRECTIONS = ScenarioAttribute("directions")
PALETTES = ScenarioAttribute("palette")
SCENARIOS = list(__SCENARIOS.keys())


def get_scenario_type(scenario: str):
    return __SCENARIOS[scenario]["type"]
