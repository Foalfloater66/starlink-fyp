# color palette reference: https://www.codecademy.com/article/seaborn-design-ii

import seaborn as sns

_SCENARIOS = {
    "Landlocked": {
        "type": "Geographical",
        "directions": ["East", "West"]
    },
    "Coastal": {
        "type": "Geographical",
        "directions": ["East", "West", "North", "South"]
    },
    "Polar": {
        "type": "Latitudinal",
        "directions": ["East", "West"]
    },
    "Equatorial": {
        "type": "Latitudinal",
        "directions": ["East", "West", "North", "South"]
    },
    "IntraOrbital": {
        "type": "Orbital",
        "directions": ["East", "West"]
    },
    "TransOrbital": {
        "type": "Orbital",
        "directions": ["East", "West"]
    }
}


class ScenarioAttribute(dict):
    def __init__(self, attr: str):
        self.attr = attr

    def __getitem__(self, key: str):
        return _SCENARIOS[key][self.attr]


LOG_DIRECTORY = "../../simulator/Logs/"
ALL_DIRECTIONS= ["East", "West", "North", "South"]
DIRECTIONS = ScenarioAttribute("directions")
PALETTES = ScenarioAttribute("palette")
TYPE = ScenarioAttribute("type")
SCENARIOS = list(_SCENARIOS.keys())
