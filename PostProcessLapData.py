import numpy as np
from os import listdir
from os.path import isfile, join

dstRoot = join("PluginsData", "KLPlugins", "DynLeaderboards", "laps_data")
rawRoot = join(dstRoot, "raw")

onlyfiles = [join(rawRoot, f) for f in listdir(rawRoot) if isfile(join(rawRoot, f)) and f.endswith(".txt")]

def CreateUsableDataFromRawData(fname, pos_delta):
    x = np.loadtxt(fname, delimiter=";")
    idxs = np.diff(x[..., 0], append=[1]) != 0
    x = x[idxs]

    lastIdx = len(x) - 1
    for i, (t1, t2) in enumerate(zip(x[..., 1], x[..., 1][1:])):
        if t2 < t1:
            lastIdx = i
            break

    x = x[:lastIdx+1]

    pos = x[..., 0]
    time = x[..., 1]

    assert((np.abs(np.diff(pos)) > 0).all()), "diff(pos) < 0"
    assert((np.diff(time) > 0).all()), "diff(time) < 0"
    
    if (pos[0] == 1):
        pos[0] == 0
    if (pos[-1] == 0):
        pos[-1] == 1
    
    mpos = pos.copy()
    if (pos[0] > 0.9):
        mpos += (1 - pos[0])
        mpos[mpos >= 1] -= 1

    new_pos = np.arange(0, 1 + pos_delta, pos_delta)
    new_time = np.interp(new_pos, mpos, time)

    if (pos[0] > 0.9) :
        if "Silverstone" in fname:
            new_pos -= 0.0209485
        elif "Spa" in fname:
            new_pos -= 0.0036425
        else:
            raise Exception(f"{fname} starts at pos {pos[0]}") 
        new_pos[new_pos <= 0] += 1.000001

    if (pos[-1] < 0.1) :
        if "Silverstone" not in fname or "Spa" not in fname:
            raise Exception(f"ends at pos {pos[-1]:.5f}") 
    data = np.array([new_pos, new_time]).T
    np.savetxt(fname.replace("raw\\", ""), data, delimiter=";", fmt="%.8f")

for file in onlyfiles:
    try:
        CreateUsableDataFromRawData(file, 0.005)
    except Exception as err:
        print(f"ERROR: Failed to process file {file}.\nMSG: {err}\n")