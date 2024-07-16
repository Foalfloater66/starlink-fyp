import seaborn as sns
import matplotlib.pyplot as plt
import pandas as pd
import os
import json
import numpy as np
from typing import List
import warnings

from constants import LOG_DIRECTORY


def load_csv_data(relative_path: str):
    # TODO: should be named load_csv_data
    """Load CSV data."""
    path = os.path.join(LOG_DIRECTORY, relative_path)
    assert os.path.isfile(path)
    return pd.read_csv(path)


def load_json_data(relative_path: str):
    """Load JSON data."""
    path = os.path.join(LOG_DIRECTORY, relative_path)
    assert os.path.isfile(path), f"Path does not exist: {path}"
    with open(path) as file:
        data = json.load(file)
    return pd.DataFrame(data["latencies"])  # LMAO WHY IS IT JUST LATENCIES...
