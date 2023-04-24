import config_phys

if config_phys.SYSTEM == 'Arenberg': # EPL
    TOY_CONTROLLER = 'ONTRAK ADU208' # 'ONTRAK ADU208' 'TDT_RP2'
    devAtten  = 'Scale' # 'Scale' or 'TDT_PA5'

if devAtten  == 'Scale':
    MAX_ATTN = 100
elif devAtten  == 'TDT_PA5':
    MAX_ATTN = 120

import os
import __main__
twoAFC = 'PsychoBaby-2AFC'
try:
    is2AFC = os.path.basename(__main__.__file__)[:len(twoAFC)] == twoAFC
    # print 'Got is2AFC, and it is ',is2AFC
except:
    # this happens if running from IDLE
    print('hw_pb.py: error evaluating is2AFC. Are you running from IDLE?')
    is2AFC = False
if is2AFC:
    # 2AFC uses 'SoundCard'
    devOutput = 'SoundCard' # 'SoundThread'=SoundDLL, 'SoundCard'=PyGame
    peakOutput = 32000.0
    SOUNDCARD_RIGHT = 1
    SOUNDCARD_LEFT = 0
    TOY_CONTROLLER = '' # empty string also disables toy code (not used for 2AFC)
else:
    devOutput = 'SoundThread' # 'SoundThread'=SoundDLL, 'SoundCard'=PyGame
    #devAtten  = 'Scale' # [7/15/13 ALL use Scale] Horn version (and eventually all) not using PA5
