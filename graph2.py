# Import libraries
import numpy as np
from scipy.stats import nbinom
import matplotlib
import matplotlib.pyplot as plt
from matplotlib import cm

matplotlib.rcParams["font.sans-serif"] = "Times New Roman"
matplotlib.rcParams["font.size"] = 13

# Compute CDF for the maximum multipath array and the number of trials.
# We characterise this situation with a negative binomial distribution,
# https://docs.scipy.org/doc/scipy/reference/generated/scipy.stats.nbinom.html 
max_multipath_arr = np.arange(1, 21)    # maximum index of the randomly selected multipath.
n = 5                                   # number of successes needed to congest the link.
trials_arr = np.arange(n, 100)          # number of executed routes (trials)

cdf_matrix = np.zeros(shape=(len(max_multipath_arr), len(trials_arr)))
for idx, max_multipath in enumerate(max_multipath_arr):
    p = 1/max_multipath                 # probability of a success.
    cdf_matrix[idx, :] = nbinom.cdf(trials_arr, n, p)


# Plot the data on a 3-dimensional graph
fig = plt.figure(figsize=(11, 8), constrained_layout=True)
ax = fig.add_subplot(111, projection="3d")
ax.view_init(elev=20, azim=135)
ax.set_box_aspect(aspect=None, zoom=0.7)
x_axis, y_axis = np.meshgrid(trials_arr, max_multipath_arr)
ax.set_xticks(np.array([5, 20, 40, 60, 80, 100]))
ax.set_xlabel("Number of Executed Routes", labelpad=10)
ax.set_yticks(np.array([1, 5, 10, 15, 20]))
ax.set_ylabel("Number of Different Possible Multipaths", labelpad=10)
ax.set_zlabel("CDF", labelpad=10) 
surf = ax.plot_surface(
    x_axis, y_axis, cdf_matrix, cmap = cm.turbo)
plt.imsave()
plt.show()