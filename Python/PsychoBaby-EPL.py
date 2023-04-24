#!/usr/bin/env python

# 19-aug-2015 - entered into Git (will now stay at "v125"); made new branch kaylah_jitter
# v125 horn sim
# v124 kaylah - insert user-specified jitter in when to start video
# v117 6/26/15 mods for new Kaylah - separate thread for video, so it can repeat with background
# v116 applied v114 mods
# v115 incorporated code from v113-kaylah-v4 and generalized for mainstream use
# v114 fix thr calc, fix horn toys
# v113 re-implements relay-controlled toys
# v112 for Mona 2/9/2015 "3I3A FC (ABC)"

# v104 observer-initiated probe trials
# v98 - add in-session IEC
# V84 - add "2AFC Baby" code, so I can use .wav with 2AFC Baby.
#       will use --forced_choice FORCED_CHOICE --ab AB --eye_track EYE_TRACK

# used if EYE_TRACK
PYGAME_OUT_BUFF_SZ = 4*1024 # Must be power of 2.
REINFORCER_MAX_TIME = 7.0
ORIENTING_STIM_DUR = 2.0

# V82 - see mona email from 31-may-14. One video contains both "background" and "Signal".
# 1st part of video is still image. 1 second later the mouth opens and closes, then still for 1 more sec.
# Will appear to be still except when the trial sound is "spoken"

# 3/3/14 - add simulation code, using old PB code as guide
# To see simulation code, do case insensitive search for "simulation" 
# To use SIMULATION:
# 1. make 2nd shortcut
# 2. add " --simulation" to end of Target
#
# v61 - let main frame create 2nd fram (in v60 the app creates the 2nd frame)
# V60 Mona
# V57 - refactor? don't see any obvious way to combine the 3

# Spectrum data is:
# 1. flat-top windowed with 50% overlap
#    - even if self-stimulated, because we may not have control of sample rates
#    - tested, and got exact same results with flat-top vs. no windowing (self-stim with no leakage)
# 2. RMS averaged (square-root of mean of mag-squared)

# always window, because we may not always have control over sample rates, and
# if input srate != output srate, you may get leakage
NSAMPS = 16384
SECONDS_TO_SKIP = .3 # give time for signal to ramp up and stabilize. 0.1 sec is probably enough.

#v1 - runs
#v2 - new structure
#v3 - sound
#v8 - get rid of old sound code
#v10 - run rand wav
#v34 rename from "PsychoBaby-Horn" to "Soundwav Baby"

# constants used in this file only
ENAB_MOVIE =  False
END_TRIAL_DLG_X_OFFSET=400 # about 4.5 inches to right

# DISABLE 3/6/15, as it makes no sense. User can specify num-to-use and num-to-ignore, this serves no purpose except to confuse.
#MAX_NUM_REVERSALS_TO_USE = 6 

############## BEGIN CONFIG ##########################################
VERSION = '2016-05-05'  # version of this program
SETTING_FILE_VERS = 2   # version of settings file this program writes
myexit_vers = '2012-03-01' #
DLL_VERS = 7            # version number of dll
############## END CONFIG ############################################

# ----------- good settings/constants are above this line ----------

# import first, as these constants may be used early on
try:
    import sys
    import config_phys
    import hw_pb as hw
except Exception as target:
    s = 'ERROR importing configuration: ' + target.msg
    print(s)
    input('Press enter to close this window.')
    sys.exit(1)

# built-ins
try:
    import os
    import fnmatch
    import sys
    import time
    import random
    import datetime
    import ctypes
    import wave
    import math
    import warnings
    #import array
    import threading # sound DLL
    import traceback
except ImportError as target:
    print('Software configuration error: ' + target.msg)
    print('Please email %s for assistance.' % config_phys.CONTACT_EMAIL)
    input('Press enter to close this window.')
    sys.exit(1)

# globals
g_frame = None
g_player = None
g_backgnd_video_active = False
g_foregnd_video_active = False
g_max_mov_dur = 30.0 # in seconds, will be updated below

def excepthook(type, value, tb):
    now = datetime.datetime.now()
    message = '{dt:%Y-%m-%d %H:%M:%S} uncaught exception in swbaby, vers {v}.\n'.format(
        v=VERSION, dt=now)
    message += ''.join(traceback.format_exception(type, value, tb))

    error_file_name = 'ERROR-{date:%Y-%m-%d}.txt'.format(date=now)
    error_file_path = os.path.abspath(error_file_name)
    with open(error_file_path, 'a') as f:
        f.write(message)
        f.write('\n')
    print(message)

    message = 'SOFTWARE ERROR, PLEASE send {0} to {1}'.format(
        error_file_path, config_phys.CONTACT_EMAIL)
    if g_frame:
        message += ' and RESTART this program.'
        ErrorDlg(g_frame, message)
    else:
        print(message)
        input('Press enter to continue.') # needed

sys.excepthook = excepthook

try:
    import wx
    import wx.lib.agw.pygauge as PG
    if 1:#ENAB_MOVIE:
        import wx.media
except ImportError as target:
    print(('Software configuration error: %s. You need to install or update the wx package.' % str(target)))
    print(('Please email %s for assistance.' % config_phys.CONTACT_EMAIL))
    input('Press enter to close this window.')
    sys.exit(1)

try:
    import numpy
except ImportError as target:
    print('Software configuration error. You need to install the numpy package.')
    print(('Please email %s for assistance.' % config_phys.CONTACT_EMAIL))
    input('Press enter to close this window.')
    sys.exit(1)

try:
    import matplotlib.pyplot as plt
except ImportError as target:
    print('Software configuration error. You need to install the matplotlib package.')
    print('Please email %s for assistance.' % config_phys.CONTACT_EMAIL)
    input('Press enter to close this window.')
    sys.exit(1)

try:
    import win32api
    import win32process
    import win32con
except ImportError as target:
    print('Software configuration error. You need to install the pywin32 package.')
    print('Please email %s for assistance.' % config_phys.CONTACT_EMAIL)
    input('Press enter to close this window.')
    sys.exit(1)

# locally developed modules
try:
    from phys_base import settings_v2 as settings
    from psychobaby_v19 import *
    from phys_base.phys import *
    import pb
except ImportError as target:
    print('Software configuration error: %s.' % str(target))
    print('Please email %s for assistance.' % config_phys.CONTACT_EMAIL)
    input('Press enter to close this window.')
    sys.exit(1)

if hw.TOY_CONTROLLER == 'TDT_RP2' and config_phys.ON_NET:
    tdt_sys3.rcoDir = 'Z:\\dev\\py-module-dev\\Tdt'

if config_phys.DEV_DLL:
    analog_io_fn = "z:\\dev\\swbaby\\SoundThread24\\Release\\SoundThread.dll" # must build with USE24BIT
else:
    analog_io_fn = os.path.join(sys.prefix,'SWBaby_DLLs','SoundThread.dll')
# from error_codes.h
ERR_OVERRUN=29
ERR_TIMEOUT=30
ERR_DSC_CREATE=38

dll_loaded = False
try:
    # http://docs.python.org/library/ctypes.html
    # Instances of this class represent loaded shared libraries. Functions in these libraries use the standard C calling convention, and are assumed to return int.
    # The Python global interpreter lock is released before calling any function exported by these libraries, and reacquired afterwards.
    analog_io = ctypes.CDLL(analog_io_fn)
    analog_io.SetDebugMode(0)
    dll_loaded = True
    # print 'loaded dll'
except WindowsError as xxx_todo_changeme6: # GOOD
    (errno, strerror) = xxx_todo_changeme6.args # GOOD
    print('WindowsError loading "%s": %s' % (analog_io_fn, strerror))
except Exception as target: # good
    print('ERROR loading %s: %s' % (analog_io_fn, str(target)))
if not dll_loaded:
    input('Press enter to close this window.')
    sys.exit(1)


# *** BEGIN NOTE *** these structures are defined here, but used only by settings module
# tuple format: (param_name, units, convFactor)
nostepParams = [
                ('Num tones per trial','',1),
                ('Randomize within trial', '', 'CHECKBOX'),
                ('Duration','ms',1e-3),
                ('Time between tones','ms',1e-3),
                ('Disable warning if wav too short', '', 'CHECKBOX'),
                ('Disable warning if wav too long (bad idea!)', '', 'CHECKBOX'),
                ('Chan 2 continuous', '', 'CHECKBOX'),
                ('Wav folder', '', 'FILENAME'),
                ('Calibration file', '', 'FILENAME'),
                ('Video file', '', 'FILENAME'),
                ('Video sound delay', 'ms', 1e-3),
                ('Video offset jitter', 'ms', 1e-3),
                ('Movie width', 'pixels', 1), # EYE_TRACK
                ('Movie edge dist', 'pixels', 1.0), # EYE_TRACK
                ('Box width', 'pixels', 1.0), # EYE_TRACK
                ('Box edge dist', 'pixels', 1.0), # EYE_TRACK
                ('Orienting cal max intensity', 'dB SPL', 1), # EYE_TRACK
                ('Orienting intensity', 'dB SPL', 1), # EYE_TRACK
                ('Movie cal max intensity', 'dB SPL', 1), # EYE_TRACK
                ('Movie intensity', 'dB SPL', 1), # EYE_TRACK
                ]


settings.nostepParams = nostepParams
settings.topParams = []
settings.stimTypes = []
settings.stimParams = []
settings.commonParams = []
settings.placeholders = []
settings.chanNames = []
# *** END NOTE *** 

# used for IEC dlg
IECParams = [('Intensity', 'dB SPL', 1.0, 40),
             ('Frequency','Hz',1.0,40),
             ('Cal max intensity', 'dB SPL', 1.0, 40),
             ('Run time', 'Seconds', 1.0, 40),
             ('V_FS', 'Volts', 1.0, 40),
             ('Mic sens', 'mV/Pa', 1.0, 40),
             ('Mic gain', 'dB', 1.0, 40),
             ('Show time history', '', 'CHECKBOX', 40),
             ]

IECVars = {'Intensity':65,
           'Frequency':1000,
           'Cal max intensity': 90,
           'Run time': 2.0,
           'V_FS': 0.793,
           'Mic sens': 50.0,
           'Mic gain': 0,
           'Show time history': 0,
           }

# used for Cal dlg
CalParams = [('Channel', '1 or 2', 1.0, 40),
             ('Wav file','','TEXT', 150),
             ]

CalVars = {'Channel':1,
           'Wav file':'noise44k_2sec.wav',
           }

CalInputUsingScopeParams = [('Attenuation', 'dB', 1.0, 40),
                            ('Frequency', 'Hz', 1.0, 40),
                            ('Run time', 'Seconds', 1.0, 40),
                            ('V_FS', 'Volts', 1.0, 40),
                            ('Show time history', '', 'CHECKBOX', 40),
                            ]

CalInputUsingScopeVars = {'Attenuation': 3,
                          'Frequency': 1000,
                          'Run time': 2.0,
                          'V_FS': 0.793,
                          'Show time history': 0,
                          }

CalInputUsingAcousCalParams = [
                            ('Run time', 'Seconds', 1.0, 40),
                            ('V_FS', 'Volts', 1.0, 40),
                            ('Acoustic output', 'dB SPL', 1.0, 40),
                            ('Mic sens', 'mV/Pa', 1.0, 40),
                            ('Mic gain', 'dB', 1.0, 40),
                            ('Show time history', '', 'CHECKBOX', 40),
                            ]

CalInputUsingAcousCalVars = {
                          'Run time': 2.0,
                          'V_FS': 0.793,
                          'Acoustic output': 94.0,
                          'Mic sens': 50.0,
                          'Mic gain': 0,
                          'Show time history': 0,
                          }

# syllables are now in the .txt file
##syllables = ['ba - body', 'cha - chop', 'da - dot', 'fa - fob',
##             'ga - got', 'ha - hot', 'dzha - jock', 'ka - cot',
##             'la - lot', 'ma - mom', 'na - not', 'pa - pot',
##             'ra - rock', 'sa -sock', 'sha - shop', 'ta - top',
##             'tha - th as in thing', 'THa - th as in the, that',
##             'va - vox', 'wa - watt, wasp', 'ja - yacht', 'za - zombie',
##             'zha - end of rouge, middle of measure, vision', 'Other',
##             'A and V different', ]
SyllableParams = [
    ('Syllable','','CHOICE', 300),
    ('Other', 'Fill in if you heard something not on list', 'TEXT', 300),
    ]

SyllableVars = {
    'Syllable':'',
    'Other': '',
    }

def load_wav_mov_syllable(parent, wav_folder, list_file):
    """ open file of wav and movie filenames, format is:
        wav mov

    num_rows, wav_fnames, mov_fnames, syllables = load_wav_mov_syllable(parent, wav_folder, list_file)

    >>> num_rows, wav_fnames, mov_fnames, syllables = load_wav_mov_syllable(None, 'doc_and_unit_test_files', 'kaylah_files2.txt')
    >>> num_rows
    3
    >>> wav_fnames
    ['GA1.wav', 'DA1.wav', 'FA1.wav']
    >>> mov_fnames
    ['GA1.mov', 'DA1.mov', 'FA1.mov']
    >>> syllables
    ['ba - body', 'cha - chop', 'da - dot']
    >>> num_rows, wav_fnames, mov_fnames, syllables = load_wav_mov_syllable(None, 'doc_and_unit_test_files', 'kaylah_files.txt')
    >>> num_rows
    3
    >>> wav_fnames
    ['GA1.wav', 'DA1.wav', 'FA1.wav']
    >>> mov_fnames
    ['GA1.mov', 'DA1.mov', 'FA1.mov']
    >>> syllables
    []
    """
    empty_ret_val = (0, [], [], [])
    if not list_file:
        ErrorDlg(parent, 'SW Error from load_wav_mov_syllable - file not specified')
        return empty_ret_val
    path = os.path.join(wav_folder, list_file)
    n_rows = 0
    A_files = []
    B_files = []
    syllables = []
    try:
        f = open(path, 'rU')
        while True:
            line = f.readline()
            if not line:
                # print 'ran out of data'
                break
            line = line.rstrip() # chop off \n
            if not line:
                # print 'blank line'
                continue
            if line[-4:] != '.mov':
                # syllable
                syllables.append(line)
            else:
                # filesnames
                row = fn_str_to_list(line)# 'a.wav', 'b.wav', 'a.wav', '1.wav'
                n = len(row)
                if n != 2:
                    ErrorDlg(parent, 'ERROR: Expecting 2 columns, but got %d columns in row %d (file %s)' %
                             (n, n_rows+1, list_file))
                    return empty_ret_val
                A_files.append(row[0])
                B_files.append(row[1])
                n_rows += 1
        f.close()
        return n_rows,A_files,B_files,syllables
    except:
        WarnDlg(parent, 'Error reading file of Kaylah file names %s' % path)
        return empty_ret_val

def load_mov_section_info(parent, wav_folder, info_file):
    """ open file of wav section info, format is like:
        VidStim-HD 720p.mov
        BA1.mov,466.6666666667,866.6666666667
        BA2.mov,1800,900
        BI1.mov,3166.6666666667,933.3333333333

    mov_file, mov_fnames, offsets, durations = load_mov_section_info(parent, wav_folder, info_file)

    >>> mov_file, mov_fnames, offsets, durations = load_mov_section_info(None, 'doc_and_unit_test_files', 'kaylah_mov_specs.txt')
    >>> mov_file
    'VidStim-HD 720p.mov'
    >>> mov_fnames
    ['BA1.mov', 'BA2.mov', 'BI1.mov']
    >>> offsets
    [467, 1800, 3167]
    >>> durations
    [867, 900, 933]
    """
    global g_max_mov_dur
    path = os.path.join(wav_folder, info_file)
    n_rows = 0
    mov_fnames = []
    offsets = []
    durations = []
    max_mov_dur = 0
    try:
        f = open(path, 'rU')
        mov_file = f.readline()
        if not mov_file or mov_file[-5:-1].lower() != '.mov':
            return '', [], [], []
        mov_file = mov_file.rstrip() # remove \n
        while True:
            line = f.readline()
            if not line:
                # print 'ran out of data'
                break
            line = line.rstrip() # chop off \n
            if not line:
                # print 'blank line'
                continue
            #print line
            row = line.split(',')
            n = len(row)
            if n != 3:
                ErrorDlg(parent, 'ERROR: Expecting 3 columns, but got %d columns in row %d (file %s)' %
                         (n, n_rows+1, info_file))
                return '',[],[],[]
            mov_fnames.append(row[0])
            offsets.append(int(0.5+float(row[1])))
            dur = int(0.5+float(row[2]))
            if dur > max_mov_dur:
                max_mov_dur = dur
            durations.append(dur)
            n_rows += 1
        f.close()
        g_max_mov_dur = max_mov_dur*1e3 # ms to sec
        return mov_file,mov_fnames,offsets,durations
    except:
        WarnDlg(parent, 'Error reading mov info file %s' % path)
        return '',[],[],[]

# ----- move into libraries -----------
def verify_wav_files_exist(folder, filenames):
    missing_files = []
    for fn in filenames:
        try:
            snd = wave.open(os.path.join(folder, fn), 'r')
            snd.close()
        except:
            missing_files.append(fn)
    return missing_files

def verify_calibrated(cal_table, wav_files):
    missing_cal_entries = []
    for wav_file in wav_files:
        if wav_file not in cal_table:
            missing_cal_entries.append(wav_file)
    return missing_cal_entries

##def create_abx_list(num_A_wavs):
##    # list of tuples: (index_into_AB, B_is_first, third_same_as_2nd)
##    abx_list = []
##    for i in range(num_A_wavs):
##        for j in range(2):
##            for k in range(2):
##                abx_list.append((i,j,k))
##    random.shuffle(abx_list)
##    return abx_list

def create_shuffled_index_list(num_A_wavs):
    # list of indexes: (index_into_ABC)
    shuffled_index_list = []
    for i in range(num_A_wavs):
        shuffled_index_list.append(i)
    random.shuffle(shuffled_index_list)
    return shuffled_index_list

def index_to_row_col(index, n_rows, n_cols):
    if index >= n_rows*n_cols:
        return -1,-1
    if index < 0:
        return -1,-1
    row = index / n_cols
    col = index % n_cols
    return row,col

def longest_wav_dur(parent, wav_folder, wav_files):
    sample_rate = 0
    max_n_samps = 0
    for wav_file in wav_files:
        # print 'wav_file="%s"' % wav_file
        wav_path = os.path.join(wav_folder, wav_file)
        try:
            snd = wave.open(wav_path, 'r')
            if snd.getsampwidth() != 2:
                ErrorDlg(parent, 'ERROR: wave file %s has %d byte samples. I need 2-byte samples.' %
                              (wav_file, snd.getsampwidth()))
                return -1
            if not sample_rate:
                # first wav determines sample rate
                sample_rate = snd.getframerate()
            elif sample_rate != snd.getframerate():
                ErrorDlg(parent, 'ERROR: sample rates must be identical. I got %s and %s.' %
                              (sample_rate, snd.getframerate()))
                return -1
            n_wav_samps = snd.getnframes()
            if n_wav_samps > max_n_samps:
                max_n_samps = n_wav_samps
            snd.close()
        except IOError as xxx_todo_changeme2: # GOOD
            (errno, strerror) = xxx_todo_changeme2.args # GOOD
            ErrorDlg(parent, '1 IOError opening "%s": %s' % (wav_path, strerror))
            return -1
        except Exception as target: # good
            ErrorDlg(parent, '1 ERROR opening %s: %s' % (wav_path, str(target)))
            return -1
        except: # in case exception not of type Exception (rare)
            ErrorDlg(parent, '1 UNKNOWN ERROR opening %s' % wav_path)
            return -1
    if not sample_rate:
        return 0.0
    else:
        return max_n_samps/float(sample_rate)
        
#-------------- OVERRIDES -----------------

class EnabProbe_Validator(wx.PyValidator):
    #dlg.control['Enable probe trials'].SetValidator(EnabProbe_Validator())
    def __init__(self):
        wx.PyValidator.__init__(self)

    def Clone(self):
        # Every validator must implement the Clone() method.
        return EnabProbe_Validator()

    def Validate(self, win):
        ctrl = self.GetWindow()
        if win.control['Enable probe trials'].GetValue():
            if not win.control['Probe wav files'].GetValue():
                wx.MessageBox(
                    'You have probe trials enabled, but no probe wav files.',
                    'Error')
                ctrl.SetBackgroundColour('red')
                ctrl.SetFocus()
                ctrl.Refresh()
                return False

        return True

    def TransferToWindow(self):
        return True

    def TransferFromWindow(self):
        return True

#-------------- END OVERRIDES -------------------------

class ThreadClass(threading.Thread):
    def run(self):
        #print "%s says begin thread at time: %s" % \
        #      (self.getName(), datetime.datetime.now())
        #print 'thread: g_debugMode=', \
        #       ctypes.c_int.in_dll(analog_io, "g_debugMode").value

##        pid = win32api.GetCurrentProcessId()
##        tid = win32api.GetCurrentThreadId()
##        print 'pid and tid of THREAD =',pid,tid
##        handle = win32api.OpenProcess(win32con.PROCESS_ALL_ACCESS, True, pid)
##        print 'THREAD pri class =',win32process.GetPriorityClass(handle)

        # The Python global interpreter lock is released before calling function and reacquired afterwards.
        stat = 0
        err_str = ''
        try:
            stat = analog_io.Play()
            # print '## audio thread ending ###'
        except WindowsError as xxx_todo_changeme3:
            (errno, strerror) = xxx_todo_changeme3.args
            err_str = 'WindowsError from analog_io.Play "%s": %s' % (errno, strerror)
        except Exception as target:
            err_str = 'ERROR from analog_io.Play: %s' % str(target)
            print() 
        except: # in case exception not of type Exception (rare)
            err_str = 'UNKNOWN ERROR from analog_io.Play'
        if err_str:
            print(err_str)
            ErrorDlg(g_frame, err_str)
        if stat:
            if stat == ERR_OVERRUN:
                ErrorDlg(g_frame, 'OVERRUN Error from analog_io.Play')
            elif stat == ERR_TIMEOUT:
                ErrorDlg(g_frame, 'TIMEOUT Error from analog_io.Play')
            else:
                ErrorDlg(g_frame, 'ERROR from analog_io.Play: %s' % stat)

if analog_io == None:
    print('ERROR:'+analog_io_fn+' cannot be loaded.')
    input('Press enter to close this window.')
    exit(1)

#----------------- video -----------------------------------------------
video_lock = threading.Lock() # prevent 2 threads from calling same video object at same time
class VideoThreadClass(threading.Thread):

    def __init__(self, movie_to_sound_delay_ms, video_offset_jitter_ms, output_period_ms, video_offsets, video_lengths, backgnd_mov_index_to_long_video_index):
        self.video_offset_jitter_ms = video_offset_jitter_ms
        self.output_period_ms = output_period_ms
        self.movie_head_start_ms = movie_to_sound_delay_ms % output_period_ms # from movie start until next out buff
        self.video_offsets = video_offsets
        self.video_lengths = video_lengths
        self.backgnd_mov_index_to_long_video_index = backgnd_mov_index_to_long_video_index
        super(VideoThreadClass, self).__init__()

    def run(self):
        SEEK_TIME_ALLOWANCE = 200 # allow 200 ms to seek
        global g_backgnd_video_active # higher-level version of video_lock
        if not g_player:
            print('NO g_player')
            return
        timeWillStart = ctypes.c_int(0)
        nMsBeforeStart = ctypes.c_int(0)
        nMsDelay = ctypes.c_int(0)
        while True:
            try:
                stat = analog_io.WhenNextBackgnd(ctypes.pointer(timeWillStart), ctypes.pointer(nMsBeforeStart), ctypes.pointer(nMsDelay))
                time_to_play = time.clock() # in sec, will become valid below
            except: # WindowsError: exception: access violation reading 0x00000000
                # the sound thread has stopped
                break
            if stat:
                print('Error from analog_io.WhenNextBackgnd: %s' % stat)
                break

            # print 'BG: after WhenNextBackgnd, nMsDelay=%s now+nMsBeforeStart=%.3f' % (nMsDelay.value, time_to_play+1e-3*nMsBeforeStart.value)
            video_offset_delay_ms = self.video_offset_jitter_ms - nMsDelay.value
            
            # 1. wait until self.movie_head_start_ms ms before next
            if nMsBeforeStart.value > self.movie_head_start_ms:
                msToWait = nMsBeforeStart.value - self.movie_head_start_ms
            else:
                msToWait = nMsBeforeStart.value + (self.output_period_ms-self.movie_head_start_ms)
            # msToWait is based on video start offset == 0. If offset is 100ms, we will wait
            msToWait += video_offset_delay_ms
            time_to_play += 1e-3*msToWait # time_to_play is now valid

            if msToWait > SEEK_TIME_ALLOWANCE:
                # print '%.3f BG 1. msToWait=%d SEEK_TIME_ALLOWANCE=%d' % (time.clock(), msToWait, SEEK_TIME_ALLOWANCE)
                msToWait -= SEEK_TIME_ALLOWANCE
                time.sleep(1e-3*msToWait)
                msToWait = SEEK_TIME_ALLOWANCE
                # print '%.3f BG 2. msToWait=%d SEEK_TIME_ALLOWANCE=%d' % (time.clock(), msToWait, SEEK_TIME_ALLOWANCE)
            
            next_index = ctypes.c_int.in_dll(analog_io, "g_backgnd_index").value
            # print '%.3f BG: next_index=%d' % (time.clock(), next_index)
            if g_foregnd_video_active or next_index < 0:
                #print '%.3f BG: will be signal or no-signal next, g_foregnd_video_active=%s next_index=%s' % (
                #    time.clock(), g_foregnd_video_active, next_index)
                time.sleep(0.1+1e-3*msToWait) # wait until foreground calls Play() (give it an extra .1 sec)
                t_timeout = time.clock() + g_max_mov_dur + 1.0 # give an extra 1 sec
                while g_foregnd_video_active:
                    time.sleep(0.1)
                    if time.clock() > t_timeout:
                        # timeout! something is wrong
                        print('############## TIMEOUT waiting for g_foregnd_video_active#############')
                        # ErrorDlg(self, 'TIMEOUT waiting for g_foregnd_video_active')
                        break
                    
            else:
                next_index = self.backgnd_mov_index_to_long_video_index[next_index] # convert to index within long video
                next_offset = self.video_offsets[next_index] + video_offset_delay_ms # in ms
                # print '%.3f BG about to seek: next_offset=%s' % (time.clock(), next_offset)
                g_backgnd_video_active = True
                if video_lock.locked():
                    print('############ LOCKED, WILL NEED TO WAIT #######')
                with video_lock:
                    g_player.Seek(next_offset) #wx.FromStart

                # returns up to .9 ms early, up to 6.5 ms late (usually late by less than .5 ms) 7/20/15
                # to be more accurate, could return early, then check time.clock() in tight loop, but this
                # would not protect against whatever causes 6.5 ms late, so
                # this is about as good as you can get under Windows7
                time_to_sleep = time_to_play - time.clock()
                if time_to_sleep < 0.0:
                    print('Movie Play is LATE ***********************************')
                else:
                    time.sleep(time_to_sleep)
                # 2. start movie
                #g_backgnd_video_active = True # 11/20/15 moved this up to before seek
                # print '%.3f BG about to play' % (time.clock(), )
                if video_lock.locked():
                    print('############ LOCKED, WILL NEED TO WAIT #######')
                with video_lock:
                    g_player.Play()
                time.sleep(1e-3*self.video_lengths[next_index]) # sleep until video segment is done playing
                if video_lock.locked():
                    print('############ LOCKED, WILL NEED TO WAIT #######')
                with video_lock:
                    g_player.Pause()
                g_backgnd_video_active = False
        #print '#### video thread ending ###'
        return

#----------------------- BEGIN BABY FEEDBACK (May move) ----------------------

class BabyFeedback:
    def __init__(self, parent, width, height, srate): # 500, 300

        # import pygame - WILL NOT BE AVAIL TO OTHER METHODS IN THIS CLASS!

        self.parent = parent
        self.center = width/2, height/2
        self.frame_width = width
        self.frame_height = height
        self.srate = srate

        self.orientation_atten = 50.0
        self.movie_atten = 50.0
        self.movies = []
        self.n_movies = 0

        self.orienting_dur = ORIENTING_STIM_DUR
        
        self.BLACK = 0, 0, 0
        self.WHITE = 255, 255, 255
        self.BACKGND_COLOR = 64, 64, 64 # dark grey (75% between white and black)

# 10/15/2013 - remove yellow
        self.random_colors = [(255, 0, 0), (0, 255, 0), (0, 0, 255),
#                              (255, 255, 0), (255, 0, 255), (0, 255, 255),
                                              (255, 0, 255), (0, 255, 255),
                              (128, 0, 0), (0, 128, 0), (0, 0, 128),
#                              (128, 128, 0), (128, 0, 128), (0, 128, 128),
                                              (128, 0, 128), (0, 128, 128),
                              ]
        self.num_random_colors = 10# 12

        self.min_radius = 1
        self.max_radius = 100

        pygame.display.init() # don't do pygame.init() - it will keep pygame.mixer.init() from being executed

        # create graphical window
        self.screen = pygame.display.set_mode((width, height)) # creates and returns Surface
        pygame.display.set_caption('')

        self._make_orienting_sound()

        self.whiteout()

    def _make_orienting_sound(self):
        self.nPointsTotal = int(self.srate * self.orienting_dur)

        if hw.devAtten == 'Scale':
            mult = numpy.power(10.0, self.orientation_atten/-20.0)
        elif hw.devAtten == 'TDT_PA5':
            mult = 1.0

        # SFM
        carrierF = 800.0 # 500Hz 
        modF = 0.5
        freqDev = -600.0
        slope = 2.0 * numpy.pi * modF / self.srate
        slope2 = 2.0 * numpy.pi * carrierF / self.srate
        self.wave = (freqDev/modF) * numpy.sin(numpy.arange(0.0, slope*self.nPointsTotal, slope))
        self.wave += numpy.arange(0.0, slope2*self.nPointsTotal, slope2)
#        self.wave = hw.peakOutput * mult * numpy.sin(self.wave)
        self.wave = 32000.0 * mult * numpy.sin(self.wave)

        rf = float(0.100) # 100ms
        if rf > 0.0:
            nRamp = int(self.srate * rf) # number of points in ramp
            if nRamp > self.nPointsTotal/2:
                nRamp = self.nPointsTotal/2
            ramp = numpy.arange(0.0, 1.0, 1.0/nRamp)
            self.wave[0:nRamp] *= ramp
            ramp = numpy.arange(1.0, 0.0, -1.0/nRamp)
            self.wave[self.nPointsTotal-nRamp:self.nPointsTotal] *= ramp

        self.myarr = numpy.zeros(dtype=numpy.float32, shape=(self.nPointsTotal,2))
#        self.myarr[:self.nPointsTotal,hw.SOUNDCARD_LEFT] = self.wave[0:self.nPointsTotal]
        self.myarr[:self.nPointsTotal,0] = self.wave[0:self.nPointsTotal]

        self.sound = pygame.sndarray.make_sound(self.myarr.astype(numpy.int16))
        self.sound.set_volume(1.0)

    def update_vars(self, orientation_atten, movie_atten, movie_folder,
                    movie_width=448, movie_pix_from_side=0,
                    box_width=450, box_pix_from_side=0):

        # movie
        self.movie_width = int(movie_width)
        self.movie_height = int(self.movie_width/1.333333)
        self.movie_rect_left = [int(movie_pix_from_side),
                                self.frame_height/2-self.movie_height/2,
                                self.movie_width,
                                self.movie_height]
        self.movie_rect_right = [self.frame_width-self.movie_width-int(movie_pix_from_side),
                                self.frame_height/2-self.movie_height/2,
                                self.movie_width,
                                self.movie_height]
        self.movie_rect_center = [self.frame_width/2-self.movie_width/2,
                                self.frame_height/2-self.movie_height/2,
                                self.movie_width,
                                self.movie_height]

        # colored boxes
        self.feedback_rect_width = int(box_width)
        self.feedback_rect_height = int(box_width/1.3333)
        self.feedback_rect_left = [int(box_pix_from_side),
                                self.frame_height/2-self.feedback_rect_height/2,
                                self.feedback_rect_width,
                                self.feedback_rect_height]
        self.feedback_rect_right = [self.frame_width-self.feedback_rect_width-int(box_pix_from_side),
                                self.frame_height/2-self.feedback_rect_height/2,
                                self.feedback_rect_width,
                                self.feedback_rect_height]

        self.orientation_atten = orientation_atten
        self._make_orienting_sound() # in case attenuation changed
        self.movie_atten = movie_atten
        # if folder does not exist then [] is returned. didn't find anything to check if folder exists
        self.movies = list(all_files(movie_folder, '*.mpg', single_level=True))
        self.n_movies = len(self.movies)
        if not SIMULATION and not self.n_movies:
            ErrorDlg(self.parent, 'ERROR: No .MPG movies in %s' % movie_folder)

    def grow_shrink_dot(self):
        #self.icolor = random.randint(0, self.num_random_colors-1)
        channel = self.sound.play()
        time_to_grow = self.orienting_dur / 2

        # grow
        time_begin = time.clock()
        radius = self.min_radius
        while radius <= self.max_radius:

            # draw bullseye
            red_radius = int(radius*(1./3.))
            white_radius = int(radius*(2./3.))
            black_radius = int(radius)
            self.screen.fill(self.BACKGND_COLOR)
            rect = pygame.draw.circle(self.screen, (0, 0, 0), self.center, black_radius, 0)
            rect = pygame.draw.circle(self.screen, (255, 255, 255), self.center, white_radius, 0)
            rect = pygame.draw.circle(self.screen, (255, 0, 0), self.center, red_radius, 0)
            pygame.display.flip() # show the image (2x-buffer)

            prev_radius = radius
            while radius < prev_radius+1:
                t = time.clock() - time_begin
                radius = int(0.5 + self.min_radius + (self.max_radius - self.min_radius) * (t / time_to_grow))

        # shrink            
        time_begin = time_begin + time_to_grow
        radius = self.max_radius
        while radius >= self.min_radius:

            # draw bullseye
            red_radius = int(radius*(1./3.))
            white_radius = int(radius*(2./3.))
            black_radius = int(radius)
            self.screen.fill(self.BACKGND_COLOR)
            rect = pygame.draw.circle(self.screen, (0, 0, 0), self.center, black_radius, 0)
            rect = pygame.draw.circle(self.screen, (255, 255, 255), self.center, white_radius, 0)
            rect = pygame.draw.circle(self.screen, (255, 0, 0), self.center, red_radius, 0)
            pygame.display.flip() # show the image (2x-buffer)

            prev_radius = radius
            while radius > prev_radius-1:
                t = time.clock() - time_begin
                radius = int(0.5 + self.min_radius + (self.max_radius - self.min_radius) * (1.0 - t / time_to_grow))

    def show_square(self, i):
        if i == 0:
            self.icolor = random.randint(0, self.num_random_colors-1)
        self.screen.fill(self.BACKGND_COLOR)
        if i == 0:
            pygame.draw.rect(self.screen, self.random_colors[self.icolor], self.feedback_rect_left, 0)
        else:
            pygame.draw.rect(self.screen, self.random_colors[self.icolor], self.feedback_rect_right, 0)
        pygame.display.flip() # show the image (2x-buffer)

    def whiteout(self):
        self.screen.fill(self.BACKGND_COLOR)
        pygame.display.flip() # show the image (2x-buffer)

    def yippee(self, offset, play_this_instead=''):
        # WARNING: because this calls pygame.mixer.quit(), you must call pygame.mixer.init() after this
        # play_this_instead is a special case - play this video instead
        if self.parent.analog_io:
            if self.parent.analog_io.SetAtten(9000, 9000): # -90 dB
                ErrorDlg(self.parent, 'ERROR while trying to mute sound.')
        pygame.mixer.quit()
        self.whiteout() # works 9/4/13
        if not play_this_instead and not self.n_movies:
            return
        if play_this_instead:
            movie_path = play_this_instead
        else:
            irnd = random.randint(0, self.n_movies-1)
            movie_path = self.movies[irnd]

        print('movie =',movie_path)

        m = pygame.movie.Movie(movie_path)
        #print 'size of movie:',m.get_size() # 640,480
        if play_this_instead:
            movie_rect = self.movie_rect_center
        elif offset == 0:
            movie_rect = self.movie_rect_left
        else:
            movie_rect = self.movie_rect_right
        m.set_display(self.screen, movie_rect)

        if hw.devAtten == 'Scale':
            mult = numpy.power(10.0, self.movie_atten/-20.0)
        elif hw.devAtten == 'TDT_PA5':
            mult = 1.0
        m.set_volume(mult)
        print('movie len = %.1f sec' % m.get_length())
        t_end = time.clock() + REINFORCER_MAX_TIME # FORCE STOP after this many seconds
        m.play()
        #print 'after play'
        timeout = time.clock()
        while m.get_busy():
            if time.clock() >= t_end:
                print('Force STOP')
                m.stop()
                break
            for event in pygame.event.get():
                if event.type == pygame.QUIT:
                    print('User requested EXIT')
                    m.stop()
                    break
        # self.whiteout() # DOES NOT WORK, but if I call after yippee() it does work (9/4/13)
        #pygame.mixer.init(self.srate, -16, 2, self.pygame_out_buff_sz)
        if self.parent.analog_io:
            if self.parent.analog_io.SetAtten(0, 0):
                ErrorDlg(self.parent, 'ERROR while trying to un-mute sound.')

    def end(self):
        pygame.quit()

#----------------------- END BABY FEEDBACK ----------------------------------

class ControlPanel(wx.Panel):
    
    def __init__(self, parent, frame):
        wx.Panel.__init__(self, parent, -1)

        p = self;

        if ENAB_MOVIE:
            self.SetBackgroundColour(wx.BLACK)
            self.SetForegroundColour(wx.WHITE)
        
        gbs = self.gbs = wx.GridBagSizer(5, 5)

        self.frame = frame # store it

        if ENAB_MOVIE:
            try:
                self.mc = wx.media.MediaCtrl(self, style=wx.SIMPLE_BORDER)
            except NotImplementedError:
                print('movie playback not enabled on this system')

        nSizeNumBox = 55    # n pix across for number input text field
        row = 0
        col = 0

        # controls to configure the MAX_N_PHASES phases
        row += 1
        col = 0
        al = wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL
        for label in ['Phase#', 'Name', 'Skip', 'Non-Adap', 'Up/Down']:
            gbs.Add( wx.StaticText(self, -1, label),
                     (row,col), (1,1), al|wx.ALL)
            al = wx.ALIGN_CENTER
            col += 1
        row += 1
        self.phaseType = {}
        self.phaseName = {}
        for ph in range(MAX_N_PHASES):
            self.phaseType[ph] = {}
            col = 0

            # phase number
            gbs.Add( wx.StaticText(self, -1, '%d    '%(ph+1)),
                     (row,col), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            col += 1

            # phase name (added 7/28/2014)
            self.phaseName[ph] = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
            gbs.Add( self.phaseName[ph],
                     (row,col), (1,1), wx.ALIGN_RIGHT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            col += 1

            # skip
            rb = wx.RadioButton(self, -1, '', style=wx.RB_GROUP)
            gbs.Add( rb, (row,col), (1,1), wx.ALIGN_CENTER|wx.ALL)
            self.phaseType[ph]['Skip'] = rb
            col += 1

            # non-adap
            rb = wx.RadioButton(self, -1, '')
            gbs.Add( rb, (row,col), (1,1), wx.ALIGN_CENTER|wx.ALL)
            self.phaseType[ph]['TRAIN'] = rb
            col += 1

            # up/down
            rb = wx.RadioButton(self, -1, '')
            gbs.Add( rb, (row,col), (1,1), wx.ALIGN_CENTER|wx.ALL)
            self.phaseType[ph]['UPDOWN'] = rb
            col += 1

            b = wx.Button(self, -1, 'Config phase %d' % (ph+1))
            self.Bind(wx.EVT_BUTTON, frame.ConfigPhase, b)
            gbs.Add( b, (row,col), (1,1), flag=wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL)
            col += 1

            if ph>0:
                b = wx.Button(self, -1, 'Copy from phase %d' % (ph))
                self.Bind(wx.EVT_BUTTON, frame.CopyPhase, b)
                gbs.Add( b, (row,col), (1,1), flag=wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL)
                col += 1

            row += 1

        # bounce-back target 0=don't use 1=bounce back to 1st phase (phase 0)
##        gbs.Add( wx.StaticText(self, -1, "Bounce-back target phase:  "),
##                 (row,0), (1,2), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
##        self.bounceBackTarget = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
##        gbs.Add( self.bounceBackTarget,
##                 (row,2), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
##        gbs.Add( wx.StaticText(self, -1, "0 to bounce back to prev phase"),
##                 (row,3), (1,3), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.bounceBackTarget = wx.TextCtrl(self, -1, "", size=(nSizeNumBox/2,-1))
        gbs.Add( self.bounceBackTarget,
                 (row,0), (1,1), wx.ALIGN_RIGHT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        gbs.Add( wx.StaticText(self, -1, "Bounce-back target phase (0 to bounce back to prev phase)"),
                 (row,1), (1,5), wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        # param boxes
        row += 1

        subjRow = row
        gbs.Add( wx.StaticText(self, -1, "Subject number:  "),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.subjectNumber = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.subjectNumber,
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        gbs.Add( wx.StaticText(self, -1, "Current phase:  "),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.currentPhase = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.currentPhase,
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        gbs.Add( wx.StaticText(self, -1, "Trial number:  "),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.trialNumber = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.trialNumber,
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

        self.trialNumber2 = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.trialNumber2,
                 (row,2), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        gbs.Add( wx.StaticText(self, -1, "  Num reversals used:  "),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.numReversalsUsed = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.numReversalsUsed,
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        if SIMULATION:
            gbs.Add( wx.StaticText(self, -1, "  Num tries:  "),
                     (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            self.numTries = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
            gbs.Add( self.numTries,
                     (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
            row += 1

        # Total, Last5 rates
        row += 1
        gbs.Add( wx.StaticText(self, -1, "Total"),
                 (row,1), (1,1), wx.ALIGN_CENTER|wx.ALL)
        gbs.Add( wx.StaticText(self, -1, "Last 5"),
                 (row,2), (1,1), wx.ALIGN_CENTER|wx.ALL)
        row += 1

        gbs.Add( wx.StaticText(self, -1, "Hit rate:  "),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.hitRateTotal = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.hitRateTotal,
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.hitRateLast5 = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.hitRateLast5,
                 (row,2), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        gbs.Add( wx.StaticText(self, -1, "False alarm rate:  "),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.falseAlarmRateTotal = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.falseAlarmRateTotal,
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.falseAlarmRateLast5 = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.falseAlarmRateLast5,
                 (row,2), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        gbs.Add( wx.StaticText(self, -1, "Probe hit rate:  "),
                 (row,0), (1,1), wx.ALIGN_RIGHT|wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.probeHitRateTotal = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.probeHitRateTotal,
                 (row,1), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        self.probeHitRateLast5 = wx.TextCtrl(self, -1, "", size=(nSizeNumBox,-1))
        gbs.Add( self.probeHitRateLast5,
                 (row,2), (1,1), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)
        row += 1

        if EYE_TRACK:
            # gauge to indicate time remaining to inform program which side the baby looked
            self.time_bar = PG.PyGauge(self, -1, size=(150,25),style=wx.GA_HORIZONTAL)
            self.time_bar.SetValue(0)
            self.time_bar.SetBarColor(wx.RED)
            self.time_bar.SetBackgroundColour(wx.GREEN)
            self.time_bar.SetBorderColor(wx.BLACK)
            gbs.Add( self.time_bar,
                     (row,0), (1,2), wx.ALIGN_LEFT |wx.ALIGN_CENTER_VERTICAL|wx.ALL)

        # control buttons
        row = subjRow # put next to subject num
        col = 4

        self.start_button = wx.Button(self, -1, "Start")
        self.Bind(wx.EVT_BUTTON, frame.Start, self.start_button)
        gbs.Add( self.start_button, (row,col), (1,2), flag=wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL)
        row += 1
        self.start_button.SetFocus() # make this button default (safe)

        b = wx.Button(self, -1, "Next phase")
        self.Bind(wx.EVT_BUTTON, frame.SkipToNextPhase, b)
        gbs.Add( b, (row,col), (1,2), flag=wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL)
        row += 1

        self.meas_iec_button = wx.Button(self, -1, "Meas IEC")
        self.Bind(wx.EVT_BUTTON, frame.meas_iec, self.meas_iec_button)
        gbs.Add( self.meas_iec_button, (row,col), (1,2), flag=wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL)
        row += 1

        b = wx.Button(self, -1, "Abort")
        self.Bind(wx.EVT_BUTTON, frame.Abort, b)
        gbs.Add( b, (row,col), (1,2), flag=wx.ALIGN_LEFT|wx.ALIGN_CENTER_VERTICAL)
        row += 1

        if ENAB_MOVIE:
            #gbs.Add(self.mc, (1,65), (30,30))#, flag=wx.EXPAND)
            gbs.Add(self.mc, (1,20), (25,25))#, flag=wx.EXPAND)
            self.Bind(wx.media.EVT_MEDIA_STOP, frame.MovieFin, self.mc)

        p.SetSizerAndFit(gbs)
        self.SetClientSize(p.GetSize())

    # *_bar() used if self.time_bar exists

    def init_bar(self, fTrialDur):
        self.time_total = fTrialDur
        self.time_bar_begin = time.clock()
        self.time_bar.SetValue(0)
        self.time_bar.Refresh()
        wx.Yield()
        
    def init_bar_begin(self):
        self.time_bar_begin = time.clock()

    def update_bar(self):
        # return 1 if time-out
        self.bar_time = time.clock() - self.time_bar_begin # time elapsed
        if self.bar_time > self.time_total:
            value = 100
            ret_val = 1 # timeout
        else:
            value = int(100.0*self.bar_time/self.time_total)
            ret_val = 0

        self.time_bar.SetValue(value)
        self.time_bar.Refresh()
        wx.Yield()
        return ret_val

#toyTimer = 0 # global, in case I put toy code in its own class

class MyFrame(PBFrame):

    def __init__(self, parent, id, title):

        global g_player

        self.analog_io = None # points to analog_io when soundthread is running

        self.SIMULATION = SIMULATION # so other files know this setting
        self.ROBOT_SIM = ROBOT_SIM
        self.USE_TRIAL_VIDEO = USE_TRIAL_VIDEO
        self.FORCED_CHOICE = FORCED_CHOICE
        self.ADULT_SUBJECT = ADULT_SUBJECT
        #print 'self.SIMULATION =',self.SIMULATION,' self.USE_TRIAL_VIDEO =', self.USE_TRIAL_VIDEO
        
        if ENAB_MOVIE:
            frameSize = wx.Size(2000, 600)
        else:
            h = 570
            if MAX_N_PHASES > 5:
                h += (MAX_N_PHASES-5)*27
            frameSize = wx.Size(650, h)
        PBFrame.__init__(self, parent, title, frameSize)
        # soonest place you can call ErrorDlg(self, 'test')

        pid = win32api.GetCurrentProcessId()
        #tid = win32api.GetCurrentThreadId()
        #print 'pid and tid of MAIN =',pid,tid
        handle = win32api.OpenProcess(win32con.PROCESS_ALL_ACCESS, True, pid)
        if not handle:
            WarnDlg(self, 'WARNING: Unable to get process handle.')
        else:
            win32process.SetPriorityClass(handle, win32process.HIGH_PRIORITY_CLASS)
            if win32process.GetPriorityClass(handle) != win32process.HIGH_PRIORITY_CLASS:
                WarnDlg(self, 'WARNING: Unable to set priority to HIGH.')
            #else:
            #    print 'Priority set to HIGH.'

        self.currPath = os.getcwd()
        self.settings_file = ''
        self.CreateStatusBar()

        self.sample_rate = 0 # used instead of self.srate
        self.gen_flat_top(NSAMPS)

        # EYE_TRACK
        self.pygame_out_buff_sz = PYGAME_OUT_BUFF_SZ
        self.pygame_srate = 44100

        self.controlPanel = ControlPanel(self, self)

        self.hwnd = self.GetHandle()

        self.abort = 0
        self.bRunning = 0
        self.bDevOutputInitd = 0 # not used for Threaded DirectSound BUT I DO USE FOR EYE_TRACK (which uses pygame)
        self.tPlusKey = 0 # time the "+" key was hit
        self.tPlusKey2 = 0 # time the "+" key was hit (C/DLL time)
        self.tForceToy = 0
        self.bInRun = False
        self.bInFCDlg = False
        self.FCDlg_key_pressed = ''

        self.n_probes = 0
        self.toys = 0

        self.max_dB_SPL = {}
        self.max_dB_SPL[0] = {}
        self.max_dB_SPL[1] = {}

        self.voltage_input_full_scale = 0.924

        self.ForcedChoiceParams = [('Choice', ' ', 1.0, 40), ]
        self.ForcedChoiceVars = {'Choice':0, }

        # used for End Trial Dialog
        if hw.TOY_CONTROLLER == 'ONTRAK ADU208':
            self.EndTrialDlgParams = [('Enable toys','','CHECKBOX',50),
                                      ('Enable DVD','','CHECKBOX',50),]
            self.EndTrialDlgVars = {'Enable toys':True,
                                    'Enable DVD':True,}
        else:
            self.EndTrialDlgParams = []
            self.EndTrialDlgVars = {}

        self.varStimFirst = {
                             'nostep': {
                                        'Num tones per trial': 1,
                                        'Randomize within trial': False,
                                        'Duration': 1.0,
                                        'Time between tones': 1.0,
                                        'Chan 2 continuous': True,
                                        'Disable warning if wav too short': False,
                                        'Disable warning if wav too long (bad idea!)': False,
                                        'Wav folder': 'C:\\Documents and Settings\\All Users\\Documents\\Studies\\SRD\\WAV Files\\10dB depth',
                                        'Calibration file': 'wav-cal-chan1',
                                        'Video file': '',
                                        'Video sound delay': 2.0,
                                        'Video offset jitter': 0.2,
                                        #'Movie folder': 'C:\\Documents and Settings\\brandon_2\\My Documents\\VasantMovies',
                                        #'Movie folder': '',
                                        'Movie width': 448,     # EYE_TRACK
                                        'Movie edge dist': 0,   # EYE_TRACK
                                        'Box width': 450,       # EYE_TRACK
                                        'Box edge dist': 0,     # EYE_TRACK
                                        'Orienting cal max intensity': 100, # EYE_TRACK
                                        'Orienting intensity': 90,          # EYE_TRACK
                                        'Movie cal max intensity': 100,     # EYE_TRACK
                                        'Movie intensity': 90,              # EYE_TRACK
                                        }
                             }
        if ABX or AB:
            del self.varStimFirst['nostep']['Num tones per trial']
            nostepParams.remove(('Num tones per trial','',1))
            #settings.nostepParams.remove(('Num tones per trial','',1))
        if not EYE_TRACK:
            del self.varStimFirst['nostep']['Movie width']
            nostepParams.remove(('Movie width','pixels',1))
            del self.varStimFirst['nostep']['Movie edge dist']
            nostepParams.remove(('Movie edge dist', 'pixels', 1.0))
            del self.varStimFirst['nostep']['Box width']
            nostepParams.remove(('Box width', 'pixels', 1.0))
            del self.varStimFirst['nostep']['Box edge dist']
            nostepParams.remove(('Box edge dist', 'pixels', 1.0))
            del self.varStimFirst['nostep']['Orienting cal max intensity']
            nostepParams.remove(('Orienting cal max intensity', 'dB SPL', 1))
            del self.varStimFirst['nostep']['Orienting intensity']
            nostepParams.remove(('Orienting intensity', 'dB SPL', 1))
            del self.varStimFirst['nostep']['Movie cal max intensity']
            nostepParams.remove(('Movie cal max intensity', 'dB SPL', 1))
            del self.varStimFirst['nostep']['Movie intensity']
            nostepParams.remove(('Movie intensity', 'dB SPL', 1))
        if not REPEATING_VIDEO:
            del self.varStimFirst['nostep']['Video offset jitter']
            nostepParams.remove(('Video offset jitter', 'ms', 1e-3))

        # params are not stepped
        self.varStimStep = {'x': [],
                            'y': []}
        self.varStimLast = {'x': [],
                            'y': []}

        # study-specific variables are placed at the bottom of train1Params
        self.train1Params = [('Trial duration', 'sec',1.0,40),
                             ('Trial delay', 'sec',1.0,40),
                             ('Min toy duration', 'sec',1.0,40),
                             ('Play video once for every', 'trials', 1.0, 40),
                             ('Intensity', 'dB SPL', 1.0, 40), # Signal intensity
                             ('Background intensity', 'dB SPL', 1.0, 40),
                             ('Background2 intensity', 'dB SPL', 1.0, 40),
                             ('No-signal intensity', 'dB SPL', 1.0, 40),
                             #('Max number of trials','','INT',40),
                             ('Min number of signal trials','','INT',40),
                             ('Min number of no-signal trials','','INT',40),
                             ('Min number of probe trials','','INT',40),
                             ('Toy requires response','','CHECKBOX',50),
                             ('Give feedback','','CHECKBOX',50),
                             ('Background wav files', '', 'TEXT', 280), # study-specific
                             ('Background2 wav files', '', 'TEXT', 280), # study-specific
                             ('Signal wav files', '', 'TEXT', 280), # study-specific
                             ('No-signal wav files', '', 'TEXT', 280), # study-specific
                             ('Probe wav files', '', 'TEXT', 280), # study-specific
                             ('Random intensities', '', 'TEXT', 280), # will pick once of these for signal intensity
                             ]
        if not MULTI_TRIAL_VIDEO_FILES:
            self.train1Params.remove(('No-signal wav files', '', 'TEXT', 280))
            self.train1Params.remove(('No-signal intensity', 'dB SPL', 1.0, 40))
        if not ABX:
            self.train1Params.remove(('Give feedback','','CHECKBOX',50))
        if FORCED_CHOICE:
            #self.train1Params.remove(('No-signal intensity', 'dB SPL', 1.0, 40))
            self.train1Params.remove(('Min number of no-signal trials','','INT',40))
            #self.train1Params.remove(('No-signal wav files', '', 'TEXT', 280))
        if not EYE_TRACK:
            self.train1Params.remove(('Play video once for every', 'trials', 1.0, 40))

        self.trainVars = {}
        self.trainVars[0] = {'Trial duration':4.0,
                             'Trial delay': 0.0,
                           'Min toy duration':2.0,
                           'Play video once for every': 0,
                           'Intensity':65, # Signal intensity
                           'Background intensity':65,
                           'Background2 intensity':65,
                           'No-signal intensity':65,
                           #'Max number of trials':10,
                           'Min number of signal trials':5,
                           'Min number of no-signal trials':5,
                           'Min number of probe trials':5,
                           'Toy requires response':1,
                           'Give feedback':1,
                           'Background wav files': '44k_250.wav', # study-specific #'standard_1_1.wav,standard_1_2.wav,standard_1_3.wav'
                           'Background2 wav files': 'standard_1_1.wav', # study-specific
                           'Signal wav files': '44k_100-1k.wav', # study-specific # 'mixed_1_1.wav,mixed_1_2.wav,mixed_1_3.wav'
                           'No-signal wav files': '44k_100-1k.wav', # study-specific # 'mixed_1_1.wav,mixed_1_2.wav,mixed_1_3.wav'
                           'Probe wav files': '', # study-specific # 'mixed_1_1.wav,mixed_1_2.wav,mixed_1_3.wav'
                           'Random intensities': '',

                           'Bounce back if reach "Max number of trials"': False,
                           'Bounce back if miss': 3,
                           'trials in a row': False,
                           'signals in a row': False,
                           'Minimum bounce backs before abort': 3,
                           'Allow final bounce back after': 12,

                           'nStoppingOption':0, # 0: check no-sig and sig 1: check trials 2: only max/min number of trials
                           'nCorrectNoSigNeeded':4, # nXcatchCorrect
                           'nNoSigTrialsToCheck':5, # nYcatchCorrect
                           'nCorrectSigNeeded':4,   # nXsignalCorrect
                           'nSigTrialsToCheck':5,   # nYsignalCorrect
                           'nCorrectProbeNeeded':0,
                           'nProbeTrialsToCheck':5,
                           'nCorrectTrialsNeeded':4,# nXtrialCorrect
                           'nTrialsToCheck':5,      # nYtrialCorrect
                           'Max number of trials':100,
                           # IF nIncorrectProbeNeeded OF THE LAST nProbeToCheckForMiss PROBE TRIALS ARE MISSED:
                           #    [ ] Stop
                           #    [ ] Bounce back
                           'nIncorrectProbeNeeded':100,
                           'nProbeToCheckForMiss':100,
                           'Stop':0,
                           'Bounce back': 0,

                           'nStoppingOption2':0,
                           'nCorrectNoSigNeeded2':4, # nXcatchCorrect
                           'nNoSigTrialsToCheck2':5, # nYcatchCorrect
                           'nCorrectSigNeeded2':4,   # nXsignalCorrect
                           'nSigTrialsToCheck2':5,   # nYsignalCorrect
                           'nCorrectProbeNeeded2':0,
                           'nProbeTrialsToCheck2':5,
                           'nCorrectTrialsNeeded2':4,# nXtrialCorrect
                           'nTrialsToCheck2':5,      # nYtrialCorrect
                           'Max number of trials2':100,
                           'nIncorrectProbeNeeded2':100,
                           'nProbeToCheckForMiss2':100,
                           'Stop2':0,
                           'Bounce back2': 0,
                           }
        if not MULTI_TRIAL_VIDEO_FILES:
            del self.trainVars[0]['No-signal wav files']
            del self.trainVars[0]['No-signal intensity']
        if not ABX:
            del self.trainVars[0]['Give feedback']
        if FORCED_CHOICE:
            #del self.trainVars[0]['No-signal intensity']
            del self.trainVars[0]['Min number of no-signal trials']
            #del self.trainVars[0]['No-signal wav files']
        if not EYE_TRACK:
            del self.trainVars[0]['Play video once for every']

        for i in range(1, MAX_N_PHASES):
            self.trainVars[i] = self.trainVars[0].copy()

        self.upDownParams = [('Trial duration', 'sec',1.0,40),
                             ('Trial delay', 'sec',1.0,40),
                        ('Min toy duration', 'sec',1.0,40),
                             ('Play video once for every', 'trials', 1.0, 40),
                        ('Max number of trials','','INT',40),
                             ('Step wav rows','','CHECKBOX',50),
                         ('Background intensity', 'dB SPL', 1.0, 40),
                         ('Background2 intensity', 'dB SPL', 1.0, 40),
                        ('Initial intensity', 'dB SPL', 1.0, 40),
                        ('Initial row', ' ', 1.0, 40),
                        ('Stop if intensity goes up to','dB SPL',1.0,40),
                        ('Stop if intensity goes down to','dB SPL',1.0,40),
                        ('Stay in if out of bounds','','CHECKBOX',50),
                         ('Give feedback','','CHECKBOX',50),

                         ('Background wav files', '', 'TEXT', 280), # study-specific
                         ('Background2 wav files', '', 'TEXT', 280), # study-specific
                         ('Signal wav files', '', 'TEXT', 280), # study-specific
                         ('Probe wav files', '', 'TEXT', 280), # study-specific
                        
                        ('Up/Down parameters','','STATIC_TEXT',120),
                        ('Correct','','INT',40),
                        ('Incorrect','','INT',40),
                        ('Initial step size','dB',1.0,40),
                        ('Min step size','dB',1.0,40),
                        ('Max step size','dB',1.0,40),
                        ('Use','reversals','INT',40),
                        ('Ignore','reversals','INT',40),
                        ]
        if not ABX:
            self.upDownParams.remove(('Give feedback','','CHECKBOX',50))
##        if FORCED_CHOICE and AB:
##            self.upDownParams.remove(('Stop if intensity goes up to','dB SPL',1.0,40))
##            self.upDownParams.remove(('Stop if intensity goes down to','dB SPL',1.0,40))
##        else:
##            self.upDownParams.remove(('Initial row', ' ', 1.0, 40))
##            self.upDownParams.remove(('Stay in if out of bounds','','CHECKBOX',50))
        if not EYE_TRACK:
            self.upDownParams.remove(('Play video once for every', 'trials', 1.0, 40))

        self.upDownVars = {}
        self.upDownVars[0] = {'Trial duration':4.0,
                              'Trial delay': 0.0,
                           'Min toy duration':2.0,
                              'Play video once for every': 0,
                           'Max number of trials':20,
                              'Step wav rows': 0,
                           'Background intensity':65,
                           'Background2 intensity':65,
                           'Initial intensity':50,
                           'Initial row':2,
                           'Stop if intensity goes up to':80,
                           'Stop if intensity goes down to':20,
                           'Stay in if out of bounds': 0,
                           'Give feedback':1,

                           'Background wav files': '44k_250.wav', # study-specific #'standard_1_1.wav,standard_1_2.wav,standard_1_3.wav'
                           'Background2 wav files': 'standard_1_1.wav', # study-specific
                           'Signal wav files': '44k_100-1k.wav', # study-specific # 'mixed_1_1.wav,mixed_1_2.wav,mixed_1_3.wav'
                           'Probe wav files': '', # study-specific # 'mixed_1_1.wav,mixed_1_2.wav,mixed_1_3.wav'
                           
                           'Correct':2,
                           'Incorrect':1,
                           'Initial step size':5,
                           'Min step size':2,
                           'Max step size':10,
                           'Use':5,
                           'Ignore':2,
                           }

        if not ABX:
            del self.upDownVars[0]['Give feedback']
##        if FORCED_CHOICE and AB:
##            del self.upDownVars[0]['Stop if intensity goes up to']
##            del self.upDownVars[0]['Stop if intensity goes down to']
##        else:
##            del self.upDownVars[0]['Initial row']
##            del self.upDownVars[0]['Stay in if out of bounds']
        if not EYE_TRACK:
            del self.upDownVars[0]['Play video once for every']

        for i in range(1, MAX_N_PHASES):
            self.upDownVars[i] = self.upDownVars[0].copy()

        self.catchAndProbeVars = {}
        self.catchAndProbeVars[0] = {'Random block size':0,
                                  'Enable No-signal trials':1,
                                  'No-sig ratio no-sig':1,
                                  'No-sig ratio sig':1,
                                  'Enable probe trials':0,
                                  'Treat probes as no-signals':0,
                                  'Probe ratio probe':1,
                                  'Probe ratio sig':1,
                                  'Probe intensity':70,
                                  
                                  }
        for i in range(1, MAX_N_PHASES):
            self.catchAndProbeVars[i] = self.catchAndProbeVars[0].copy()

        # for bounce back (had to remove some vars because of name collision)
        self.nBouncedBackFromThisPhase = {}
        self.nBouncedBackToThisPhase = {}
        self.nTrialsSoFar = {}
        self.nSignal = {}
        self.nSignalByRow = {} # for ABX - track for each row of wav file matrix
        self.nCatch = {}
        self.nProbe = {}
        self.nCorrectSignal = {}
        self.nCorrectSignalByRow = {} # for ABX - track for each row of wav file matrix
        self.nIncorrectCatch = {}
        self.nProbeResponses = {} # was nCorrectProbe until we added 'Treat probes as no-signals'
        self.nTrialNumAll = {}
        self.nPhaseAll = {}
        self.bCorrectAll = {}
        self.nTrialTypeAll = {}
        self.latencyByRow = {} # for ABX - track for each row of wav file matrix

        if EYE_TRACK:
            # ----- BABY FEEDBACK -----
            pygame.mixer.init(self.pygame_srate, -16, 2, self.pygame_out_buff_sz)

        display1 = wx.Display(0) # first monitor
        if display1.GetCount() > 1:
            print('have second monitor')
            display2 = wx.Display(1)
            usable_display_size = display2.GetClientArea() # same as phys. if taskbar: height is reduced
            baby_window_width = usable_display_size[2]
            baby_window_height = usable_display_size[3]
            left2,top2,width2,height2 = display2.GetGeometry() # left2,top2 is location of 2nd monitor
        else:
            print('only one monitor')
            display2 = None
            usable_display_size = display1.GetClientArea() # (0, 0, 1280, 1024) - Left,Top,Width,Height
            baby_window_width = usable_display_size[2]/2
            baby_window_height = usable_display_size[3]/2

        if EYE_TRACK:
            if display2:
                new_win = wx.Frame(self, -1, 'Baby Frame', (left2,top2), (width2,baby_window_height))
                new_win_handle = new_win.GetHandle()
                new_win.Show()
                # in next 2 lines: if too big, some of the movie will be clipped
                # if too small, will see area around active rectangle that is not updated - looks messy
                baby_window_width -= 8# - 8 close to exact fit on win7
                baby_window_height -= 27 # -27 close to exact fit on win7
                # the following places "Baby Screen" inside "Baby Frame", overriding the frame title
                os.putenv('SDL_WINDOWID', str(new_win_handle))
            self.baby_feedback = BabyFeedback(self, baby_window_width, baby_window_height, self.pygame_srate)

        elif USE_TRIAL_VIDEO:
            # ----- BABY WINDOW -----
            if display2:
                # print 'have second monitor - use all of it'
                # print 'width2,height2:',width2,height2 # same as phys dimensions, even if taskbar present
                self.movie_frame = wx.Frame(self, -1, 'Baby Frame', (left2,top2), (width2,baby_window_height))
            else:
                # print 'only one monitor'
                self.movie_frame = wx.Frame(self, -1, 'Baby Frame', size=wx.Size(900, 600))

            self.movie_frame.player = wx.media.MediaCtrl(parent=self.movie_frame,
                                                         szBackend=wx.media.MEDIABACKEND_WMP10)
            g_player = self.movie_frame.player
            self.movie_frame.Show(True)
            #self.Bind(wx.media.EVT_MEDIA_STOP, self.OnMediaStop)
            self.Bind(wx.media.EVT_MEDIA_LOADED, self.OnMediaLoaded)
            #self.Bind(wx.media.EVT_MEDIA_FINISHED, self.OnMediaFinished)
            self.movie_timer = wx.Timer(self)
            self.Bind(wx.EVT_TIMER, self.OnMovieTimer)

        # kind of obsolete
        self.dlg_mask1a = None # serves as flag to indicate that we have 2nd monitor and to use it
        self.dlg_mask2a = None

        w = 200
        h = 200
        self.dlg_operator = SimpleModelessDlg(self, '', '')
        self.dlg_operator.SetSize((w,h))
        if ADULT_SUBJECT:
            self.dlg_feedback = SimpleModelessDlg(self, '', '')
            self.dlg_feedback.SetSize((w,h))
            self.dlg_init_trial = SimpleModelessDlg(self, 'Hit enter to begin 1st trial',
                                                    'Hit enter to begin 1st trial')
            self.dlg_init_trial.SetSize((w,h))
            if display2:
                # usable_display_size = display2.GetClientArea() # same as phys. if taskbar: height is reduced
                left2,top2,width2,height2 = display2.GetGeometry() # left2,top2 is location of 2nd monitor
                # print 'width2,height2:',width2,height2 # same as phys dimensions, even if taskbar present
                self.center2H = width2/2 + left2
                self.center2V = height2/2 + top2
                self.dlg_mask1a = 1 # used as flag: 2nd monitor present
                x = self.center2H - w/2
                y = self.center2V - h/2
                self.dlg_feedback.SetPosition((x,y))
                self.dlg_init_trial.SetPosition((x,y))
        else:
            if display2:
                self.dlg_mask1a = 1 # used as flag: 2nd monitor present
                self.center2H = width2/2 + left2
                self.center2V = height2/2 + top2

        # build menu bar
        self.mainmenu = wx.MenuBar()

        menu = wx.Menu()
        
        item = menu.Append(-1, "E&xit", "Terminate the program")
        self.Bind(wx.EVT_MENU, self.TimeToQuit, item)
        wx.App_SetMacExitMenuItemId(item.GetId())
        self.mainmenu.Append(menu, '&File')

        menu = wx.Menu()
        item = menu.Append(-1, "Stimulus", "Set stim settings")
        self.Bind(wx.EVT_MENU, self.OnSettings, item)
        menu.AppendSeparator()
        item = menu.Append(-1, "&Save all settings", "Save all settings to file")
        self.Bind(wx.EVT_MENU, self.OnSaveSettings, item)
        item = menu.Append(-1, "&Load all settings", "Load all settings from file")
        self.Bind(wx.EVT_MENU, self.OnLoadSettings, item)
        self.mainmenu.Append(menu, '&Settings')

        menu = wx.Menu()
        item = menu.Append(-1, "Calibrate", "Calibrate")
        self.Bind(wx.EVT_MENU, self.OnCalibrate, item)
        item = menu.Append(-1, "Load cal file", "Load calibration file")
        self.Bind(wx.EVT_MENU, self.OnLoadCalFile, item)

        item = menu.Append(-1, "Cal input using oscilloscope", "Input calibration using oscilloscope")
        self.Bind(wx.EVT_MENU, self.input_cal_using_oscope, item)

        item = menu.Append(-1, "Cal input using acoustic calibrator", "Input calibration using acoustic calbrator")
        self.Bind(wx.EVT_MENU, self.input_cal_using_acoustic_calibrator, item)

        self.mainmenu.Append(menu, '&Calibration')

        menu = wx.Menu()
        item = menu.Append(-1, "&About", "More information about this program")
        self.Bind(wx.EVT_MENU, self.OnAbout, item)
        self.mainmenu.Append(menu, '&Help')

        self.SetMenuBar(self.mainmenu)
        
        self.Bind(wx.EVT_CLOSE, self.OnCloseWindow)

        self.bToyOn = False

        try:
            dll_version = ctypes.c_int.in_dll(analog_io, "g_version").value
        except:
            dll_version = 0

        if DLL_VERS != dll_version:
            s = 'ERROR: SoundThread.dll is not the right version! I\'m expecting '
            s += 'version %d, and the version at %s is %d' % (DLL_VERS, analog_io_fn, dll_version)
            ErrorDlg(self, s)
            self.bRunning = 0
            return

##        TOYTIMER_ID = wx.NewId()
##        wx.EVT_TIMER(self, TOYTIMER_ID, self.TurnOffToy)
##        global toyTimer
##        # Initialise the timer - wxPython requires this to be connected to the
##        # receiving event handler
##        toyTimer = wx.Timer(self, TOYTIMER_ID)


##    def OnMediaStop(self, event):
##        print '------ OnMediaStop - not useful to code in here?'
##        # code below doesn't seem to work
##        #self.movie_frame.player.Seek(0) #, wx.FromStart)
##        #self.movie_frame.player.SetPosition(0)
##        #event.Veto()
##        ##self.movie_frame.player.Seek(0) # doesn't work - too late?
##
##    def OnMediaFinished(self, event):
##        print '------ on media finished - '
##        #self.movie_frame.player.Play() # does play again, but see flash
##        #self.movie_frame.player.Pause() # see blank (too late?)
##        # i'm pretty sure i want to catch it earlier

        if hw.TOY_CONTROLLER:
            if not self.toys:
                self.toys = Toys(self, hw.TOY_CONTROLLER)
                if not self.toys.strDevice:
                    # error initializing toys
                    self.bRunning = 0
                    return
                self.toys.TurnOff()
                print('toys are turned off')

    def OnMediaLoaded(self, event):
        print('------ OnMediaLoaded')
        #self.movie_frame.player.Pause() # doesn't work, still disappears
        if video_lock.locked():
            print('############ LOCKED, WILL NEED TO WAIT #######')
        with video_lock:
            self.movie_frame.player.Play() # how they say to use it. if you don't, you see flash of video on load.
            self.movie_frame.player.Seek(0) # make sure we are at very beginning
            self.movie_frame.player.Pause()

    def OnMovieTimer(self, event):
        global g_foregnd_video_active
        # works!
        # print '%.3f FG: ------ begin OnMovieTimer - ' % time.clock()
        # works
        if video_lock.locked():
            print('############ LOCKED, WILL NEED TO WAIT #######')
        with video_lock:
            self.movie_frame.player.Seek(0) # works! video looks fine
            self.movie_frame.player.Pause() # works!
            g_foregnd_video_active = False
        # print '%.3f FG: ------ end OnMovieTimer - ' % time.clock()

##        # pause works, but don't see effect of seek
##        self.movie_frame.player.Pause()
##        self.movie_frame.player.Seek(0)

    def ErrorDlg(self, error_str):
        ErrorDlg(self, error_str)

    def OnLoadCalFile(self, event):
        wildcard = "Calibration files (*.txt)|*.txt|" \
           "All files (*.*)|*.*"

        dlg = wx.FileDialog(
            self, message="Choose a calibration file", defaultDir=self.varStimFirst['nostep']['Wav folder'],
            defaultFile=self.varStimFirst['nostep']['Calibration file'], wildcard=wildcard, style=wx.OPEN
            )
        if dlg.ShowModal() != wx.ID_OK:
            return
        self.LoadCalFile(dlg.GetPath())

    def LoadCalFile(self, path):
        try:
            f = open(path, 'rU')
            self.max_dB_SPL = {}
            self.max_dB_SPL[0] = {}
            while True:
                line = f.readline()
                if not line:
                    # print 'ran out of data'
                    break
                line = line.rstrip() # chop off \n
                # print 'CAL LINE',line,len(line)
                if not line:
                    # print 'blank line'
                    continue
                fn,dB = line.split()
                fn = fn.strip() # make sure no whitespace on either side
                dB = dB.strip()
                self.max_dB_SPL[0][fn] = float(dB)
            self.max_dB_SPL[1] = self.max_dB_SPL[0].copy()
            self.varStimFirst['nostep']['Calibration file'] = os.path.split(path)[1]
        except:
            WarnDlg(self, 'Error loading cal file')

    def ConfigPhase(self, event):
        # OVERRIDE so that we can specify study-specific validator
        # button = event.GetEventObject()
        phase = int(event.GetEventObject().GetLabel()[-2:]) - 1 # 1st is 0 (user sees 1)
        if self.controlPanel.phaseType[phase]['TRAIN'].GetValue():
            # make sure the vars exist
            if phase not in self.trainVars:
                print('find most recent phase with info')
                src_phase = phase - 1
                while src_phase > 0:
                    if src_phase in self.trainVars:
                        break
                    else:
                        src_phase -= 1
                if src_phase < 0:
                    print('could not find prev phase train vars')
                    # let illegal key trigger exception, which will be caught
                print('copy from phase0 %d to phase0 %d' % (src_phase,phase))
                for i in range(src_phase, phase):
                    self.trainVars[i+1] = self.trainVars[i].copy()
                    self.catchAndProbeVars[i+1] = self.catchAndProbeVars[i].copy()
            dlg = SettingsPhase(self, 'Training (non-adaptive) settings',
                                self.trainVars[phase], self.train1Params, self.catchAndProbeVars[phase], phase) # passed by ref!
        elif self.controlPanel.phaseType[phase]['UPDOWN'].GetValue():
            dlg = SettingsPhase(self, 'Up/Down settings',
                                self.upDownVars[phase], self.upDownParams, self.catchAndProbeVars[phase], phase) # passed by ref!
        else:
            WarnDlg(self, 'Nothing to config for Skip')
            return

##        dlg.control['No-sig ratio sig'].SetValidator(NoSigRatio_Sig_Validator())
##        dlg.control['Probe ratio sig'].SetValidator(ProbeRatio_Sig_Validator())
        dlg.control['Enable probe trials'].SetValidator(EnabProbe_Validator())
        val = dlg.ShowModal()
        if val == wx.ID_OK:
            dlg.GetData() # will write into dlg.filenameVars, which, because pass by ref, is our copy
        dlg.Destroy()
        self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing

    def OnSettings(self, evt):
        dlg = settings.SettingsDialog(self,
                             self.varStimFirst,
                             self.varStimStep,
                             self.varStimLast) # passed by ref!
        dlg.CenterOnScreen()
        val = dlg.ShowModal()
        if val == wx.ID_OK:
            dlg.GetData() # will write into dlg.varStimFirst, which, because pass by ref, is our copy
            if EYE_TRACK:
                self.baby_feedback.update_vars(self.varStimFirst['nostep']['Orienting cal max intensity']-self.varStimFirst['nostep']['Orienting intensity'],
                                               self.varStimFirst['nostep']['Movie cal max intensity']-self.varStimFirst['nostep']['Movie intensity'],
                                               self.varStimFirst['nostep']['Wav folder'],
                                               self.varStimFirst['nostep']['Movie width'], self.varStimFirst['nostep']['Movie edge dist'],
                                               self.varStimFirst['nostep']['Box width'], self.varStimFirst['nostep']['Box edge dist'],
                                               )
        dlg.Destroy()

    def OnCalibrate(self, evt):
        dlg = ModelessDlg(self, 'Calibrate', '', ['Turn On', 'Turn Off', 'Close'], CalParams, CalVars)
        dlg.CenterOnScreen()
        dlg.Show()
        bSoundOn = 0
        while not dlg.sButt == 'Close':
            wx.Yield()
            if dlg.sButt == 'Turn On':
                dlg.sButt = '' # we got the value, now reset it
                if bSoundOn:
                    WarnDlg(self, 'Already on')
                    continue
                dlg.GetData()

                stat = analog_io.Init(self.hwnd, 0)
                if stat:
                    ErrorDlg(self, 'ERROR from analog_io.Init: %s' % stat)
                    return

                # run it
                duration = longest_wav_dur(self, self.varStimFirst['nostep']['Wav folder'], wav_fn_str_to_list(CalVars['Wav file']))
                print('cal wav duration=',duration)
                if duration > 4.0 and CalVars['Channel'] == 1:
                    WarnDlg(self, 'WARNING: that is a long wav file (%.1f seconds) to play on channel 1. You probably meant channel 2, the background channel. It will take up to %.0f seconds to stop.' % (duration, duration))
                self.sample_rate = 0 # allows each phase to have diff sample rate

                #LoadWaves(out_chan, wav_files, level, typ, stim_dur, ch2_dur, stim_gap=-1):
                # uses self.n_backgnds, self.n_backgnd2s, self.n_signals, self.n_probes
                # typ: 0=signal 1=background 2=background2 3=probe
                # typ=2 - ch2_dur is LONG

                if CalVars['Channel'] == 1:
                    self.n_backgnds = 1
                    self.n_backgnd2s = 0
                    self.n_signals = 1
                    self.n_probes = 0
                    self.n_nosignals = 0

                    stat = self.LoadWaves(out_chan=0, wav_files=wav_fn_str_to_list(CalVars['Wav file']),
                                          level=0, typ=1, stim_dur=duration, ch2_dur=0, stim_gap=0.0, is_cal=True)
                    if stat:
                        continue
                else:
                    self.n_backgnds = 1
                    self.n_backgnd2s = 1
                    self.n_signals = 1
                    self.n_probes = 0
                    self.n_nosignals = 0

                    stat = self.LoadWaves(out_chan=1, wav_files=wav_fn_str_to_list(CalVars['Wav file']),
                                          level=0, typ=2, stim_dur=0.5, ch2_dur=duration, stim_gap=0.0, is_cal=True)
                    if stat:
                        continue

                    # load silent buffer ("background") into chan 1. must do after self.LoadWaves()
                    self.c_buff[:self.buff_size] = numpy.zeros(shape=(self.buff_size,), dtype=numpy.int16)
                    stat = analog_io.LoadWav(1, 0, self.buff_size, ctypes.pointer(self.c_buff), 0)
                    if stat:
                        ErrorDlg(self, 'Error from analog_io.LoadWav: %s' % stat)
                        self.abort = 1
                        return

                self.sound_thread = ThreadClass()
                self.sound_thread.start() # invokes run() in a separate thread of control
                print('self.sound_thread.isAlive()=',self.sound_thread.isAlive())

                bSoundOn = 1
            elif dlg.sButt == 'Turn Off' or dlg.sButt == 'Close':
                if bSoundOn:
                    stat = self.StopSoundThread()
                    bSoundOn = 0
                if dlg.sButt == 'Close':
                    break
                dlg.sButt = '' # we got the value, now reset it
            time.sleep(0.1) # FREE UP CPU ? MAKE EVENT DRIVEN?
        dlg.Destroy()

    def OnSaveSettings(self, evt):
        wildcard = "PsychoBaby settings file (*.set)|*.set|" \
           "All files (*.*)|*.*"
        dlg = wx.FileDialog(
            self, message="Save file as ...", defaultDir=self.currPath, 
            defaultFile=".set", wildcard=wildcard, style=wx.SAVE+wx.OVERWRITE_PROMPT
            )
        if dlg.ShowModal() != wx.ID_OK:
            return
        path = dlg.GetPath()
        self.currPath = path
        self.settings_file = path
        dlg.Destroy()

        # phase type buttons into strPhaseType
        strPhaseType = []
        phaseName = []
        for phase in range(MAX_N_PHASES):
            for phType in ['Skip', 'TRAIN', 'UPDOWN']:
                if self.controlPanel.phaseType[phase][phType].GetValue():
                    strPhaseType.append(phType)
                    break
            phaseName.append(self.controlPanel.phaseName[phase].GetValue())

        try:
            f = open(path, 'w')
            f.write('settingsFileVers = ' + str(SETTING_FILE_VERS) + '\n')
            f.write('strPhaseType = ' + str(strPhaseType) + '\n')
            f.write('varStimFirst = ' + str(self.varStimFirst) + '\n')
            f.write('trainVars = ' + str(self.trainVars) + '\n')
            f.write('upDownVars = ' + str(self.upDownVars) + '\n')
            f.write('catchAndProbeVars = ' + str(self.catchAndProbeVars) + '\n')
            f.write('phaseName = ' + str(phaseName) + '\n')
            f.write('bounceBackTarget = "%s"\n' % str(self.controlPanel.bounceBackTarget.GetValue()))
            f.write('END_OF_HEADER\n')
            f.close()
            # self.SetTitle('%s - %s' % (os.path.basename(path), self.title)) # this info can bias the user
            self.SetTitle(self.title)
        except IOError as xxx_todo_changeme4:
            (errno, strerror) = xxx_todo_changeme4.args
            estr = "I/O error(%s): %s." % (errno, strerror)
            if errno == 13:
                estr += ' The destination file may be open by another program.'
            ErrorDlg(self, estr)
        except:
            ErrorDlg(self, 'unknown error while storing settings file')

    def LoadSettings(self, path):
        # execfile(path)  # the file is a series of assignment statements
        f = open(path, 'rU')

        params = {}
        stat = ReadHeader(self, 0, f, path, params)
        if stat:
            return stat
        #print '\nafter, params=',params
        strPhaseType = params['strPhaseType']
        self.varStimFirst = params['varStimFirst']
        self.trainVars = params['trainVars']
        self.upDownVars = params['upDownVars']
        self.catchAndProbeVars = params['catchAndProbeVars']
        if 'phaseName' not in params:
            phaseName = []
        else:
            phaseName = params['phaseName']
        if 'bounceBackTarget' in params:
            self.controlPanel.bounceBackTarget.SetValue(params['bounceBackTarget'])
        else:
            self.controlPanel.bounceBackTarget.SetValue('')

        for phase in range(MAX_N_PHASES):
            try:
                # when new variables are added, check them here
                self.add_setting_if_missing(self.trainVars[phase], 'Background intensity', 56)
                self.add_setting_if_missing(self.trainVars[phase], 'Background2 intensity', 57)
                self.add_setting_if_missing(self.trainVars[phase], 'Minimum bounce backs before abort', 3)
                self.add_setting_if_missing(self.trainVars[phase], 'Allow final bounce back after', 12)

                self.add_setting_if_missing(self.trainVars[phase], 'nIncorrectProbeNeeded', 100)
                self.add_setting_if_missing(self.trainVars[phase], 'nProbeToCheckForMiss', 100)
                self.add_setting_if_missing(self.trainVars[phase], 'Stop', 0)
                self.add_setting_if_missing(self.trainVars[phase], 'Bounce back', 0)
                self.add_setting_if_missing(self.trainVars[phase], 'nIncorrectProbeNeeded2', 100)
                self.add_setting_if_missing(self.trainVars[phase], 'nProbeToCheckForMiss2', 100)
                self.add_setting_if_missing(self.trainVars[phase], 'Stop2', 0)
                self.add_setting_if_missing(self.trainVars[phase], 'Bounce back2', 0)
                self.add_setting_if_missing(self.trainVars[phase], 'Random intensities', '')

                if ABX:
                    self.add_setting_if_missing(self.trainVars[phase], 'Give feedback', 0)
                    self.add_setting_if_missing(self.upDownVars[phase], 'Give feedback', 0)
                self.add_setting_if_missing(self.catchAndProbeVars[phase], 'Treat probes as no-signals', 0)

                self.add_setting_if_missing(self.trainVars[phase], 'nProbeTrialsToCheck', 5)
                self.add_setting_if_missing(self.trainVars[phase], 'nProbeTrialsToCheck2', 5)

                self.add_setting_if_missing(self.trainVars[phase], 'nCorrectProbeNeeded', 0)
                self.add_setting_if_missing(self.trainVars[phase], 'nCorrectProbeNeeded2', 0)
                if EYE_TRACK:
                    self.add_setting_if_missing(self.trainVars[phase], 'Play video once for every', 0)
                self.add_setting_if_missing(self.upDownVars[phase], 'Trial delay', 0)
                self.add_setting_if_missing(self.upDownVars[phase], 'Background intensity', 56)
                self.add_setting_if_missing(self.upDownVars[phase], 'Background2 intensity', 57)
                self.add_setting_if_missing(self.upDownVars[phase], 'Background wav files', '')
                self.add_setting_if_missing(self.upDownVars[phase], 'Background2 wav files', '')
                self.add_setting_if_missing(self.upDownVars[phase], 'Signal wav files', '')
                self.add_setting_if_missing(self.upDownVars[phase], 'Probe wav files', '')
                if MULTI_TRIAL_VIDEO_FILES:
                    self.add_setting_if_missing(self.trainVars[phase], 'No-signal wav files', '')
                    self.add_setting_if_missing(self.trainVars[phase], 'No-signal intensity', 65)
                    self.add_setting_if_missing(self.upDownVars[phase], 'No-signal wav files', '')
                    self.add_setting_if_missing(self.upDownVars[phase], 'No-signal intensity', 65)
                if FORCED_CHOICE and AB:
                    self.add_setting_if_missing(self.upDownVars[phase], 'Initial row', 3)
                    self.add_setting_if_missing(self.upDownVars[phase], 'Stay in if out of bounds', 0)
                self.add_setting_if_missing(self.upDownVars[phase], 'Step wav rows', 0)
                if EYE_TRACK:
                    self.add_setting_if_missing(self.upDownVars[phase], 'Play video once for every', 0)
            except:
                if phase == 0:
                    ErrorDlg(self, 'Error understanding settings file.')
                    return 1
                # just disable this one
                if len(strPhaseType) <= phase:
                    strPhaseType.append('Skip')
                    # adding past end - copy last good phase into this one?
                else:
                    strPhaseType[phase] = 'Skip'

        # when new variables are added, check them here
        self.add_setting_if_missing(self.varStimFirst['nostep'], 'Chan 2 continuous', True)

        self.add_setting_if_missing(self.varStimFirst['nostep'], 'Disable warning if wav too short', False)
        self.add_setting_if_missing(self.varStimFirst['nostep'], 'Disable warning if wav too long (bad idea!)', False)
        self.add_setting_if_missing(self.varStimFirst['nostep'], 'Calibration file', '')

        if EYE_TRACK:
            self.add_setting_if_missing(self.varStimFirst['nostep'], 'Movie width', 448)
            self.add_setting_if_missing(self.varStimFirst['nostep'], 'Movie edge dist', 0)
            self.add_setting_if_missing(self.varStimFirst['nostep'], 'Box width', 450)
            self.add_setting_if_missing(self.varStimFirst['nostep'], 'Box edge dist', 0)

        # phase type buttons from strPhaseType
        for phase in range(MAX_N_PHASES):
            if len(phaseName) > phase:
                self.controlPanel.phaseName[phase].SetValue(phaseName[phase])
            else:
                self.controlPanel.phaseName[phase].SetValue('')
            for phType in ['Skip', 'TRAIN', 'UPDOWN']:
                if strPhaseType[phase] == phType:
                    self.controlPanel.phaseType[phase][phType].SetValue(1)
                else:
                    self.controlPanel.phaseType[phase][phType].SetValue(0)

        if not SIMULATION:
            if self.varStimFirst['nostep']['Calibration file']:
                # load cal file
                path = os.path.join(self.varStimFirst['nostep']['Wav folder'],
                                    self.varStimFirst['nostep']['Calibration file'])
                self.LoadCalFile(path)

        if EYE_TRACK:
            self.baby_feedback.update_vars(self.varStimFirst['nostep']['Orienting cal max intensity']-self.varStimFirst['nostep']['Orienting intensity'],
                                           self.varStimFirst['nostep']['Movie cal max intensity']-self.varStimFirst['nostep']['Movie intensity'],
                                           self.varStimFirst['nostep']['Wav folder'],
                                           self.varStimFirst['nostep']['Movie width'], self.varStimFirst['nostep']['Movie edge dist'],
                                           self.varStimFirst['nostep']['Box width'], self.varStimFirst['nostep']['Box edge dist'],
                                           )
        return 0

   #def LoadWaves(self, out_chan, wav_files, level, typ, stim_gap=-1):
    def LoadWaves(self, out_chan, wav_files, level, typ, stim_dur, ch2_dur, stim_gap=-1, is_cal=False):
        # uses self.n_backgnds, self.n_backgnd2s, self.n_signals
        # typ: 0=signal 1=background 2=background2 3=probe
        # typ=2 - ch2_dur may be LONG
        # print 'LoadWaves: wav_files=',wav_files,' typ=',typ
        buff_index = 0
        if stim_gap < 0.0:
            stim_gap = float(self.varStimFirst['nostep']['Time between tones'])
        
        wav_folder = self.varStimFirst['nostep']['Wav folder']
        for wav_file in wav_files:
            # print 'wav_file="%s"' % wav_file
            wav_path = os.path.join(wav_folder, wav_file)
            try:
                snd = wave.open(wav_path, 'r')
                if snd.getsampwidth() != 2:
                    ErrorDlg(self, 'ERROR: wave file %s has %d byte samples. I need 2-byte samples.' %
                                  (wav_file, snd.getsampwidth()))
                    return 1
            except IOError as xxx_todo_changeme: # GOOD
                (errno, strerror) = xxx_todo_changeme.args # GOOD
                ErrorDlg(self, '2 IOError opening "%s": %s' % (wav_path, strerror))
                return 1
            except Exception as target: # good
                ErrorDlg(self, '2 ERROR opening %s: %s' % (wav_path, str(target)))
                return 1
            except: # in case exception not of type Exception (rare)
                ErrorDlg(self, '2 UNKNOWN ERROR opening %s' % wav_path)
                return 1

            if not self.sample_rate:
                # first wav determines sample rate
                self.sample_rate = snd.getframerate()
                self.n_stim_samps = int(stim_dur * self.sample_rate)
                n_gap_samps = int(stim_gap * self.sample_rate)
                self.buff_size = self.n_stim_samps+n_gap_samps
                if self.varStimFirst['nostep']['Chan 2 continuous']:
                    self.buff_size_ch2 = int(ch2_dur * self.sample_rate)
                else:
                    self.buff_size_ch2 = self.buff_size
                # record (doesn't really record anything useful. may be needed for now)
                nsamps_per_pri_buff = self.sample_rate # FOR UNUSED RECORD 1 second
                nsamps_per_sec_buff = nsamps_per_pri_buff * 4 # FOR UNUSED RECORD 4 seconds
                self.c_input_buff = (ctypes.c_float * nsamps_per_sec_buff)() # FOR UNUSED RECORD buffer to hold waveform
                if not self.n_backgnds:
                    # no backgrounds. tell DLL that we have 1. we'll have to fill it with zeros
                    # LATER: make DLL detect this and init with zeros
                    n_backgnds = 1
                else:
                    n_backgnds = self.n_backgnds

                if is_cal:
                    video_offset_jitter = 0.0 # not used during cal
                else:
                    try:
                        video_offset_jitter = self.varStimFirst['nostep']['Video offset jitter'] # negative means special test mode (only used here, use abs() everywhere else)
                    except:
                        video_offset_jitter = 0.0 # zero if not defined
                if video_offset_jitter != 0.0 and abs(video_offset_jitter) > stim_gap:
                    WarnDlg(self, 'WARNING: you cannot specify a "Video offset jitter" greater than "Time between tones"')
                    video_offset_jitter = stim_gap
                n_jitter_samps = int(video_offset_jitter * self.sample_rate) # negative means special debug mode
                stat = analog_io.AllocateBuffers(self.sample_rate, self.buff_size, n_jitter_samps, self.n_signals,
                                                 n_backgnds, self.n_probes, self.n_nosignals, self.buff_size_ch2,
                                                 nsamps_per_pri_buff, # record
                                                 nsamps_per_sec_buff, ctypes.pointer(self.c_input_buff), # record
                                                 )
                if stat:
                    ErrorDlg(self, 'Error from analog_io.AllocateBuffers: %s' % stat)
                    return 1
                if not self.n_backgnds:
                    # fill background with zeros (this may be unneeded)
                    self.c_buff = (ctypes.c_short * self.buff_size)() # buffer to hold waveform (auto-init to zeros)
                    stat = analog_io.LoadWav(1, 0, self.buff_size, ctypes.pointer(self.c_buff),
                                             0) #ctypes.byref(stream))
                    if stat:
                        ErrorDlg(self, 'Error from analog_io.LoadWav: %s' % stat)
                        return 1
            else:
                # verify same sample rate
                if self.sample_rate != snd.getframerate():
                    ErrorDlg(self,
                             'ERROR: all your wav files must be same sample rate. %s is sampled at %s, expecting %s' % (
                                 wav_file, snd.getframerate(), self.sample_rate))
                    return 1
            n_wav_samps = snd.getnframes()
            if typ == 2 and self.varStimFirst['nostep']['Chan 2 continuous']:
                n_samps_to_read = self.buff_size_ch2
                self.c_buff = (ctypes.c_short * self.buff_size_ch2)() # buffer to hold waveform
            else:
                n_samps_to_read = self.n_stim_samps
                self.c_buff = (ctypes.c_short * self.buff_size)() # buffer to hold waveform (auto-init to zeros)
            if n_wav_samps > n_samps_to_read:
                # is it long enough to bitch about?
                n_extra = n_wav_samps - n_samps_to_read
                t_extra = float(n_extra) / self.sample_rate
                if t_extra > 0.0005 and not self.varStimFirst['nostep']['Disable warning if wav too long (bad idea!)']:
                    WarnDlg(self, '%s is %.0f ms too long' % (wav_file,
                                                             1e3*t_extra))
            elif n_wav_samps < n_samps_to_read:
                n_short = n_samps_to_read - n_wav_samps
                if float(n_short)/n_samps_to_read > 0.10 and not self.varStimFirst['nostep']['Disable warning if wav too short']:
                    # more than 10% short
                    WarnDlg(self, '%s is %.0f%% too short' % (wav_file,
                                                             100.0*(n_samps_to_read-n_wav_samps)/n_samps_to_read))
                n_samps_to_read = n_wav_samps
            stream = snd.readframes(n_samps_to_read)
            sound_data = numpy.fromstring(string=stream, dtype=numpy.int16)
            self.c_buff[:n_samps_to_read] = sound_data[:]

            if level > 0:
                # level is in dB SPL
                if wav_file not in self.max_dB_SPL[out_chan]:
                    ErrorDlg(self, '%s not in chan %s cal table' % (wav_file, out_chan+1))
                    return 1
                else:
                    atten = self.max_dB_SPL[out_chan][wav_file] - level
                if atten < 0.0:
                    WarnDlg(self,
                            'Cannot get to %.1f dB SPL with %s on chan %s. The highest I can reach is %.1f dB SPL.' % (
                                level, wav_file, out_chan+1, self.max_dB_SPL[out_chan][wav_file]))
                    atten = 0.0
                atten_times100 = int(atten*100.0)
            else:
                # level is in dB attenuation
                atten_times100 = int(-level*100.0)
            #print 'load wav typ=%s atten=%.1f fn=%s' % (typ, atten_times100/100.0, wav_file)
            stat = analog_io.LoadWav(typ, buff_index, n_samps_to_read, ctypes.pointer(self.c_buff),
                                     atten_times100) #ctypes.byref(stream))
            if stat:
                ErrorDlg(self, 'Error from analog_io.LoadWav: %s' % stat)
                return 1
            buff_index += 1
            snd.close()
        return 0
        
    def Start(self, event):
        if self.bRunning:
            print('already running')
            return
        self.bRunning = 1
        self.controlPanel.meas_iec_button.Disable()

        bounceBackTargetCtrl = self.controlPanel.bounceBackTarget # the text field
        self.bounceBackTarget = bounceBackTargetCtrl.GetValue()
        err = False
        if not self.bounceBackTarget:
            self.bounceBackTarget = 0
        else:
            try:
                self.bounceBackTarget = int(self.bounceBackTarget)
            except:
                err = True
        if not err:
            if self.bounceBackTarget < 0 or self.bounceBackTarget > MAX_N_PHASES:
                err = True
        if err:
            bounceBackTargetCtrl.SetBackgroundColour('red')
            bounceBackTargetCtrl.SetSelection(0,-1)
            bounceBackTargetCtrl.SetFocus()
            bounceBackTargetCtrl.Refresh()
            ErrorDlg(self, 'Bounce back target must be an integer number between 0 and %d' % MAX_N_PHASES)
            self.bRunning = 0
            return
        bounceBackTargetCtrl.SetBackgroundColour('white')

        if USING_PYGAME and not self.bDevOutputInitd:
            # self.tSndLat = float(self.pygame_out_buff_sz) / self.pygame_srate
            self.bDevOutputInitd = 1

        if ADULT_SUBJECT or EYE_TRACK:
            hw.TOY_CONTROLLER = '' # TODO: how to specify that we don't use toys for this?

        if not SIMULATION:
            if USE_TRIAL_VIDEO:
                if MULTI_TRIAL_VIDEO_FILES:
                    video_info_file = self.varStimFirst['nostep']['Video file']
                    # in milliseconds
                    mov_file, self.video_names, self.video_offsets, self.video_lengths = load_mov_section_info(
                        self,
                        self.varStimFirst['nostep']['Wav folder'],
                        video_info_file)
                    if not mov_file:
                        if os.path.splitext(video_info_file)[1].lower() != '.csv':
                            ErrorDlg(self, 'Error: the "Video file" (%s) should actually be a .CSV file that describes the video file.' % video_info_file)
                        else:
                            ErrorDlg(self, 'Error understanding file that should contain video file information: %s' % video_info_file)
                        self.bRunning = 0
                        return
                        
                    self.video_indices = {}
                    for i in range(len(self.video_lengths)):
                        self.video_lengths[i] += 100 # add 100ms, so it stops 100 ms past end (black)
                        self.video_indices[self.video_names[i]] = i
                    movie_path = os.path.join(self.varStimFirst['nostep']['Wav folder'],
                                              mov_file)
                else:
                    movie_path = os.path.join(self.varStimFirst['nostep']['Wav folder'],
                                              self.varStimFirst['nostep']['Video file'])
                if not os.path.exists(movie_path):
                    ErrorDlg(self, 'Error: video not found at %s' % movie_path)
                    self.bRunning = 0
                    return
                # self.movie_frame.Show(False) # always visible now
                if video_lock.locked():
                    print('############ LOCKED, WILL NEED TO WAIT #######')
                with video_lock:
                    self.movie_frame.player.Load(movie_path)
                # how to tell if movie loaded OK? self.movie_frame.player.Length() returns zero, even if I sleep 5 seconds

            if ENAB_MOVIE:
                # MOVIE
                movieFolder = self.varStimFirst['nostep']['Movie folder']
                if len(movieFolder):
                    print('MOVIE ENABLED')
                    self.moviePaths = []
                    self.nMovies = 0
                    for path in all_files(movieFolder, '*.avi',):
                        self.moviePaths.append(path)
                        self.nMovies += 1

        #AfxMessageBox("Warning: Your computer has not been shut down for over 45 days. To avoid time rollover error you should reboot your computer.");

        # check
        ch1_wav_files = []
        ch2_wav_files = []
        for nPhase in range(MAX_N_PHASES):
            if self.controlPanel.phaseType[nPhase]['Skip'].GetValue():
                continue
            elif self.controlPanel.phaseType[nPhase]['TRAIN'].GetValue():
                the_vars = self.trainVars[nPhase]
            elif self.controlPanel.phaseType[nPhase]['UPDOWN'].GetValue():
                the_vars = self.upDownVars[nPhase]
            else:
                WarnDlg(self, "Error: unknown phase/mode")
                continue

            ch2_wav_files += wav_fn_str_to_list(the_vars['Background2 wav files'])
            if MULTI_TRIAL_VIDEO_FILES:
                for file_spec, required in [('Signal wav files', True),
                                            ('Background wav files', True),
                                            ('No-signal wav files', True),
                                            ('Probe wav files', False),]:
                    txt_fname = the_vars[file_spec]
                    if not required and not txt_fname:
                        continue
                    if required and not txt_fname:
                        ErrorDlg(self, 'ERROR - "{0}" not specified in phase {1}'.format(file_spec, nPhase+1))
                        self.bRunning = 0
                        return
                    n_rows, wav_files, mov_files, syllables = load_wav_mov_syllable(
                        self, self.varStimFirst['nostep']['Wav folder'], txt_fname)
                    if not n_rows:
                        ErrorDlg(self, 'ERROR reading "{0}" in phase {1}'.format(txt_fname, nPhase+1))
                        self.bRunning = 0
                        return
                    ch1_wav_files += wav_files
            elif FORCED_CHOICE and AB and self.controlPanel.phaseType[nPhase]['UPDOWN'].GetValue() and the_vars['Step wav rows']:
                ch1_wav_files += wav_fn_str_to_list(the_vars['Probe wav files'])
                n_cols, n_rows, wav_files = load_wav_filenames(self,
                    self.varStimFirst['nostep']['Wav folder'],
                    the_vars['Signal wav files'])
                ch1_wav_files += wav_files

                n_cols2, n_rows2, wav_files = load_wav_filenames(self,
                    self.varStimFirst['nostep']['Wav folder'],
                    the_vars['Background wav files'])
                ch1_wav_files += wav_files
                if n_cols2 != n_cols or n_rows2 != n_rows:
                    ErrorDlg(self, 'ERROR: the Signal and Background file lists have different dimensions (%dx%d vs. %dx%d).' % (
                        n_cols, n_rows, n_cols2, n_rows2))
                    self.bRunning = 0
                    return
            elif ABC:
                max_ans = 2
                if THREE_ALT:
                    max_ans = 3
                n_rows,A_wav_files,B_wav_files,C_wav_files,answers = load_abc_wav_filenames(self,
                    self.varStimFirst['nostep']['Wav folder'], the_vars['Signal wav files'], max_ans)
                ch1_wav_files += A_wav_files
                ch1_wav_files += B_wav_files
                ch1_wav_files += C_wav_files
                if not n_rows:
                    ErrorDlg(self, 'ERROR: reading ABC wav file.')
                    self.bRunning = 0
                    return
            elif ABX and not ABC:
                n_cols, n_rows, wav_files = load_wav_filenames(self,
                    self.varStimFirst['nostep']['Wav folder'],
                    the_vars['Signal wav files'])
                ch1_wav_files += wav_files

                n_cols2, n_rows2, wav_files = load_wav_filenames(self,
                    self.varStimFirst['nostep']['Wav folder'],
                    the_vars['Background wav files'])
                ch1_wav_files += wav_files
                if n_cols2 != n_cols or n_rows2 != n_rows:
                    ErrorDlg(self, 'ERROR: the A and B file lists have different dimensions (%dx%d vs. %dx%d).' % (
                        n_cols, n_rows, n_cols2, n_rows2))
                    self.bRunning = 0
                    return
            else: # better test is to see if ['Signal wav files'] contains .txt
                ch1_wav_files += wav_fn_str_to_list(the_vars['Probe wav files'])
                ch1_wav_files += wav_fn_str_to_list(the_vars['Signal wav files'])
                ch1_wav_files += wav_fn_str_to_list(the_vars['Background wav files'])

        if not SIMULATION:
            # check for missing wav files
            missing_wav_files = verify_wav_files_exist(self.varStimFirst['nostep']['Wav folder'],
                                                   ch1_wav_files + ch2_wav_files)

            # check chan 1 cal
            missing_cal_entries = verify_calibrated(self.max_dB_SPL[0],
                                                    ch1_wav_files)

            # check chan 2 cal
            missing_cal_entries += verify_calibrated(self.max_dB_SPL[1], ch2_wav_files)

            if missing_wav_files:
                missing_wav_files = list(set(missing_wav_files))
                ErrorDlg(self, 'ERROR: Missing wav files: %s' % missing_wav_files)
            if missing_cal_entries:
                missing_cal_entries = list(set(missing_cal_entries))
                ErrorDlg(self, 'ERROR: Missing cal entries for: %s' % missing_cal_entries)
            if missing_wav_files or missing_cal_entries:
                self.bRunning = 0
                return

        wildcard = "Output file (*.txt)|*.txt|" \
           "All files (*.*)|*.*"
        dlg = wx.FileDialog(
            self, message="Enter file name to save to, or hit Cancel", defaultDir=self.currPath, 
            defaultFile=".txt", wildcard=wildcard, style=wx.SAVE+wx.OVERWRITE_PROMPT
            )
        if dlg.ShowModal() != wx.ID_OK:
            self.bRunning = 0
            return
        path = dlg.GetPath()
        self.currPath = path
        dlg.Destroy()
        print(path)

        try:
            self.outFile = open(path, 'w')
            self.outFile.write("Subject Number: %s\n" % self.controlPanel.subjectNumber.GetValue())
            self.outFile.write("Settings file: %s\n" % self.settings_file)
            if self.bounceBackTarget:
                #print 'bounce-back phase is:',self.bounceBackTarget
                self.outFile.write("Bounce-back target: %s\n" % self.bounceBackTarget)
            self.outFile.write("Program version: %s\n" % VERSION)
        except IOError as xxx_todo_changeme5:
            (errno, strerror) = xxx_todo_changeme5.args
            estr = "I/O error(%s): %s." % (errno, strerror)
            if errno == 13:
                estr += ' The destination file may be open by another program.'
            ErrorDlg(self, estr)
            self.bRunning = 0
            return
        except:
            ErrorDlg(self, 'unknown error while storing header 1')
            self.bRunning = 0
            return

        if ROBOT_SIM:
            n_sessions = 1000
            n_tries = 1
            self.simulation_output_only = 0
        elif not SIMULATION:
            # NORMAL
            n_sessions = 1
            n_tries = 1
            self.simulation_output_only = 0
        else:
            # SIMULATION
            n_tries_str = self.controlPanel.numTries.GetValue()
            if n_tries_str:
                n_tries = int(n_tries_str)
            else:
                # default to 1 if not entered
                n_tries = 1
            print('n_tries=',n_tries)
            items = self.controlPanel.subjectNumber.GetValue().split()
            if len(items) != 2:
                estr = 'ERROR: you must enter num_sessions and probability in Subject Number (To run 1000 sessions with 55%, enter: 1000 55)'
                ErrorDlg(self, estr)
                self.bRunning = 0
                return
            n_sessions = int(items[0])
            prob = float(items[1])/100.0
            if prob < 0.0 or prob > 1.0:
                estr = 'ERROR: probability must be between 0 and 100, you entered %.1f %%' % prob
                ErrorDlg(self, estr)
                self.bRunning = 0
                return
            print('n_sessions,prob,n_tries:',n_sessions,prob,n_tries)
            # code to allow differnt probabilities
            self.sim_sig_resp_prob = prob
            self.sim_nosig_resp_prob = prob
            self.sim_probe_resp_prob = prob
            self.outFile.write("SIMULATION num sessions: %d  response probability: %.0f%% num tries: %d\n" % (
                n_sessions, 100.0*prob, n_tries))
            self.simulation_output_only = (n_sessions > 100)
            sim_begin_time = time.clock()

        nSessionsCompleted = [] # num sessions completed in SIMULATION (index is phase, 1st phase is 0)
        for i in range(MAX_N_PHASES):
            nSessionsCompleted.append(0)

        # Horn 7/30/15
        highestPhasePassed = []
        terminalPhase = []
        terminalTrial = []
        for i in range(n_sessions):
            highestPhasePassed.append(-1)
            terminalPhase.append(-1)
            terminalTrial.append(-1)

        for i_session in range(n_sessions): # if SIMULATION, n_sessions will be > 1
            if SIMULATION:
                # display i_session+1 on control panel (before I used nReversalsUsed)
                if 1:#((i_session+1) % 10) == 0: # no sig speed diff (.5 sec)
                    print('session:',i_session+1)
                if 1: #SIM_TEST:
                    self.outFile.write('\n----- session %d -----\n' % (i_session+1))

            initial_phase = 0
            for i_try in range(n_tries):
                if i_try > 0:
                    if SIMULATION: #SIM_TEST:
                        print('try number %d' % (i_try+1))
                        self.outFile.write('\n--- try %d -----\n' % (i_try+1))
                    else:
                        print('################# THIS SHOULD NOT HAPPEN (i_try>0 and NOT SIMULATION')
                # if not SIMULATION, this executes once
                
                self.abort = 0

                # init bounce-back vars
                for i in range(MAX_N_PHASES):
                    self.nBouncedBackFromThisPhase[i] = 0
                    self.nBouncedBackToThisPhase[i] = 0
                    self.nTrialsSoFar[i] = 0
                    self.nSignal[i]=0
                    self.nSignalByRow[i]={}
                    self.nCatch[i]=0
                    self.nProbe[i]=0
                    self.nCorrectSignal[i]=0
                    self.nCorrectSignalByRow[i]={}
                    self.nIncorrectCatch[i]=0
                    self.nProbeResponses[i]=0
                    self.latencyByRow[i] = {}
                self.nTrialIndexAll = 0 # starts at zero

                # The following are dictionaries indexed by nPhase (1st is 0)
                self.signal_index_list = {}
                self.signal_index_list_position = {}
                self.signal_index_list_size = {}

                self.nosignal_index_list = {}
                self.nosignal_index_list_position = {}
                self.nosignal_index_list_size = {}

                self.probe_index_list = {}
                self.probe_index_list_position = {}
                self.probe_index_list_size = {}

                self.rand_intensity_list = {}
                self.rand_intensity_list_position = {}
                self.rand_intensity_list_size = {}

                sim_phase_completed = initial_phase-1 #-1; # SIMULATION: 0 if completed 1st phase (phase 0)
                phase = initial_phase
                self.nextPhase = 0 # use if non-zero # used for bounce-back: back to prev phase after bounce-back # base0, but will never be 0
                bounceback_due_to_max_trials = False
                while phase < MAX_N_PHASES:
                    if self.controlPanel.phaseType[phase]['Skip'].GetValue():
                        # print 'skipping'
                        phase += 1
                        continue
                    if self.controlPanel.phaseType[phase]['TRAIN'].GetValue():
                        self.Run(phase, 'TRAIN', self.trainVars[phase], self.catchAndProbeVars[phase])
                    elif self.controlPanel.phaseType[phase]['UPDOWN'].GetValue():
                        self.Run(phase, 'UPDOWN', self.upDownVars[phase], self.catchAndProbeVars[phase])
                    else:
                        WarnDlg(self, "Error: unknown phase/mode")
                        phase += 1
                        continue
                    self.bInRun = False
                    if self.abort == 1:
                        initial_phase = phase
                        if SIMULATION and not self.simulation_output_only:
                            self.outFile.write('reached max (?), i_try=%d will try phase %d again\n' % (i_try, initial_phase+1)) # another try?
                        # what if it got bounced-back? Then we want the higher phase
                        if SIMULATION and sim_phase_completed >= phase:
                            initial_phase = sim_phase_completed + 1 # start at phase beyond what we already passed
                            if not self.simulation_output_only:
                                self.outFile.write('  looks like we hit max in a bounce-back, so set next try to phase %d\n' % (initial_phase+1))
                            terminalPhase[i_session] = self.nextPhase
                        break # next try
                    elif self.abort == 2:
                        self.abort = 0 # user said to skip to next phase
                        phase += 1
                        continue
                    elif self.abort == 3:
                        # user said to skip to the testing phase
                        phase = 2 # testing
                        self.abort = 0
                        continue
                    elif self.abort == 4 or self.abort == 5:
                        # MAX NUMBER OF TRIALS (stop after BB): self.abort = 4
                        # MISSED X TRIALS/SIGNALS IN A ROW (may do >1 BB): self.abort = 5
                        if self.abort == 4:
                            bounceback_due_to_max_trials = True
                        
                        self.nextPhase = phase # save our place, so we can come back after completing bounce-back phase
                        if self.bounceBackTarget:
                            # horn method: bounce back to fixed phase
                            bounceToPhase = self.bounceBackTarget-1 # to target (subtract 1 because 1st is 1)
                            if phase <= bounceToPhase:
                                # illegal
                                error_str = 'Tried to bounce back to phase %d, but on phase %d. Abort.' % (bounceToPhase+1, phase+1)
                                ErrorDlg(self, error_str)
                                self.outFile.write(error_str+'\n')
                                self.abort = 1
                                break # next try (should never happen)
                        else:
                            # original method: bounce back to previous phase (BUT GO BEFORE THAT IF MARKED AS SKIP?)
                            bounceToPhase = phase-1
                            while bounceToPhase>=0 and self.controlPanel.phaseType[bounceToPhase]['Skip'].GetValue():
                                # that phase is marked as SKIP
                                bounceToPhase -= 1
                        if bounceToPhase < 0:
                            self.abort = 1
                            #initial_phase = phase # if we bounced back, restart at initial phase
                            if SIMULATION:
                                self.outFile.write('##tried to bounce back to before 1st phase, i_try=%d will try phase %d next\n' % (i_try, initial_phase+1)) # another try?
                            # if more tries left, will try again (self.abort will be cleared)
                            break # next try

                        self.nBouncedBackFromThisPhase[phase] += 1 # mark phase we're bouncing back from as being "bounced-back"
                        self.nBouncedBackToThisPhase[bounceToPhase] += 1 
                        self.nTrialsSoFar[bounceToPhase] = 0	# reset this too
                        self.nSignal[bounceToPhase] = 0
                        self.nCatch[bounceToPhase]=0
                        self.nProbe[bounceToPhase]=0
                        self.nCorrectSignal[bounceToPhase]=0
                        self.nIncorrectCatch[bounceToPhase]=0
                        self.nProbeResponses[bounceToPhase]=0
                        if ABX:
                            for i in range(len(self.nSignalByRow[bounceToPhase])):
                                self.nSignalByRow[bounceToPhase][i]=0
                                self.nCorrectSignalByRow[bounceToPhase][i]=0
                                self.latencyByRow[bounceToPhase][i]=0.0
                        if not self.simulation_output_only:
                            self.outFile.write('bounce back from phase %d to phase %d\n' % (phase+1, bounceToPhase+1))
                        phase = bounceToPhase
                        self.abort = 0
                        continue
                    elif self.abort != 0:
                        error_str = 'illegal value of self.abort: %s' % self.abort
                        ErrorDlg(self, error_str)
                        self.outFile.write(error_str+'\n')
                        self.abort = 1
                        break # next try (should never happen)

                    if sim_phase_completed < phase:
                        sim_phase_completed = phase # SIMULATION

                    if self.nextPhase:
                        msg = 'just finished GOOD bounce-back from phase %s' % (self.nextPhase+1)
                        print(msg)
                        if not self.simulation_output_only:
                            self.outFile.write(msg+'\n')

                        if self.abort == 0:
                            # this bounceback passed
                            if self.nTrialsSoFar[self.nextPhase] > terminalTrial[i_session]:
                                terminalTrial[i_session] = self.nTrialsSoFar[self.nextPhase] # will get max across all tries

                        # --- BEGIN ---
                        if bounceback_due_to_max_trials or \
                           self.nBouncedBackFromThisPhase[self.nextPhase] >= self.trainVars[self.nextPhase]['Minimum bounce backs before abort'] and \
                           self.nTrialsSoFar[self.nextPhase] >= self.trainVars[self.nextPhase]['Allow final bounce back after']:
                            # LAST BOUNCE BACK
                            if not self.simulation_output_only:
                                if bounceback_due_to_max_trials:
                                    self.outFile.write('Finished last bounce back, because we hit "Max number of trials" setting\n')
                                else:
                                    self.outFile.write('Finished last bounce back, because "Minimum bounce backs before abort" is %s and "Allow final bounce back after X trials completed" is %s\n' % (
                                        self.trainVars[self.nextPhase]['Minimum bounce backs before abort'],
                                        self.trainVars[self.nextPhase]['Allow final bounce back after']))
                            terminalPhase[i_session] = self.nextPhase
                            self.abort = 1

                            if sim_phase_completed >= phase:
                                # 13-aug-2015 we just passed BB, make sure we don't repeat it
                                initial_phase = sim_phase_completed + 1 # start at phase beyond what we already passed
                                if SIMULATION and not self.simulation_output_only:
                                    self.outFile.write("just passed BB, make sure we don't repeat it\n")

                            break # next try
                        # --- END ---
                        # return to earlier phase
                        bounceback_due_to_max_trials = False
                        phase = self.nextPhase
                        self.nextPhase = 0
                        continue
                    phase += 1

            for i in range(sim_phase_completed+1):
                nSessionsCompleted[i] += 1
            #sim_phase_completed (Horn 7/30/15)
            highestPhasePassed[i_session] = sim_phase_completed # THE ONLY PLACE UPDATED
            self.controlPanel.currentPhase.SetValue(str(phase+1)) # user sees phase that begins at 1

        # end of session loop
        if SIMULATION:
            print('********* DONE WITH SIMULATION took %.1f seconds *************' % (time.clock()-sim_begin_time))

        if 1: # to test simulation data while running interactively (why not do always?)
            self.outFile.write('\nPhase\tn_completed\n')
            for i in range(MAX_N_PHASES):
                self.outFile.write('%d\t%d\n' % (i+1,nSessionsCompleted[i]))
            self.outFile.write('\nSession\tHighest phase passed\tTerminal Phase\tTerminal trial\n')
            for i in range(n_sessions):
                self.outFile.write('%d\t%d\t%d\t%d\n' % (i+1,
                                                         highestPhasePassed[i]+1,
                                                         terminalPhase[i]+1,
                                                         terminalTrial[i],
                                                         )
                                   ) # add 1 to phase (1st is 1)

        if not SIMULATION:
            if hw.TOY_CONTROLLER:
                self.TurnOffToy()

        self.bRunning = 0
        self.outFile.close()
        self.controlPanel.meas_iec_button.Enable()

    def Run(self, nPhase, mode, var, catchAndProbeVars):
        global g_foregnd_video_active
        if not self.simulation_output_only:
            s = '\nBegin phase %d (%s) on %s\n' % (
                nPhase+1,
                self.controlPanel.phaseName[nPhase].GetValue(),
                time.strftime("%B %d, %Y %H:%M",time.localtime(time.time())))
            try:
                self.outFile.write(s)
                if mode == 'TRAIN':
                    if var['Random intensities']:
                        self.outFile.write('Chan 1 signal level = %s dB SPL\n' % var['Random intensities']) # Signal intensity
                    else:
                        self.outFile.write('Chan 1 signal level = %s dB SPL\n' % var['Intensity']) # Signal intensity
                if catchAndProbeVars['Enable probe trials']:
                    self.outFile.write('Chan 1 probe level = %s dB SPL\n' % catchAndProbeVars['Probe intensity'])
                if var['Background wav files'] and not ABX and not AB:
                    self.outFile.write('Chan 1 background level = %s dB SPL\n' % var['Background intensity'])
                if var['Background2 wav files']:
                    self.outFile.write('Chan 2 background level = %s dB SPL\n' % var['Background2 intensity'])
                if MULTI_TRIAL_VIDEO_FILES and var['No-signal wav files']:
                    self.outFile.write('No-signal level = %s dB SPL\n' % var['No-signal intensity'])

                s = ''
                if MULTI_TRIAL_VIDEO_FILES:
                    s += "subj #\tphase\t"

                s += "trial\ttrial2\tstep#\tlevel\t"

                if not SYLLABLE:
                    s += "type\tresp_lat_ms\tcorrect\t"

                if MULTI_TRIAL_VIDEO_FILES:
                    s += "WAV\t"
                else:
                    s += "file\t"

                if MULTI_TRIAL_VIDEO_FILES:
                    s += "MOV\t"

                if SYLLABLE:
                    s += "Ans\tOther"

                self.outFile.write(s+'\n')

            except IOError as xxx_todo_changeme1:
                (errno, strerror) = xxx_todo_changeme1.args
                estr = "I/O error(%s): %s." % (errno, strerror)
                if errno == 13:
                    estr += ' The destination file may be open by another program.'
                ErrorDlg(self, estr)
                self.abort = 1
                return
            # the following catches fatal (it aborts) error, but I never get tracedump. just remove it.
##            except:
##                ErrorDlg(self, 'unknown error while storing header 2')
##                self.abort = 1
##                return

        try:
            video_offset_jitter_ms = abs(int(1e3*self.varStimFirst['nostep']['Video offset jitter']))
        except:
            video_offset_jitter_ms = 0

        # setup output
        signal_wav_files = []
        self.n_signals = 0
        backgnd_wav_files = []
        self.n_backgnds = 0
        backgnd2_wav_files = []
        self.n_backgnd2s = 0
        probe_wav_files = []
        self.n_probes = 0
        nosignal_wav_files = []
        self.n_nosignals = 0

        if nPhase not in self.signal_index_list_size:
            # these may be used even if list is not
            self.signal_index_list_size[nPhase] = 0
            self.nosignal_index_list_size[nPhase] = 0
            self.probe_index_list_size[nPhase] = 0

        if mode == 'TRAIN' and var['Random intensities']:
            self.rand_intensity_list[nPhase] = create_shuffled_intensity_list(
                var['Random intensities'],
                catchAndProbeVars['Random block size'])
            self.rand_intensity_list_position[nPhase] = 0
            self.rand_intensity_list_size[nPhase] = len(self.rand_intensity_list[nPhase])
        else:
            self.rand_intensity_list_size[nPhase] = 0

        if MULTI_TRIAL_VIDEO_FILES: # new july 2015
            n_rows, signal_wav_files, mov_files, syllables = load_wav_mov_syllable(self,
                self.varStimFirst['nostep']['Wav folder'], var['Signal wav files'])
            if not n_rows:
                ErrorDlg(self, 'ERROR: reading Kaylah signal wav file.')
                self.bRunning = 0
                return
            self.n_signals = len(signal_wav_files)

            i, backgnd_wav_files, backgnd_mov_files, syllables = load_wav_mov_syllable(self,
                self.varStimFirst['nostep']['Wav folder'], var['Background wav files'])
            if not i:
                ErrorDlg(self, 'ERROR: reading Kaylah background wav file.')
                self.bRunning = 0
                return
            self.n_backgnds = len(backgnd_wav_files)

            i, nosignal_wav_files, nosignal_mov_files, syllables = load_wav_mov_syllable(self,
                self.varStimFirst['nostep']['Wav folder'], var['No-signal wav files'])
            if not i:
                ErrorDlg(self, 'ERROR: reading Kaylah nosignal wav file.')
                self.bRunning = 0
                return
            self.n_nosignals = len(nosignal_wav_files)

            probe_wav_files = []
            if catchAndProbeVars['Enable probe trials'] and var['Probe wav files']:
                i, probe_wav_files, probe_mov_files, syllables = load_wav_mov_syllable(self,
                    self.varStimFirst['nostep']['Wav folder'], var['Probe wav files'])
                if not i:
                    ErrorDlg(self, 'ERROR: reading Kaylah probe wav file.')
                    self.bRunning = 0
                    return
            self.n_probes = len(probe_wav_files)

            n_A_wavs_this_phase = n_rows # + i? NO, you want separate lists so that ratios and num wav files are NOT coupled

            # create self.backgnd_mov_index_to_long_video_index
            self.backgnd_mov_index_to_long_video_index = []
            for i in range(len(backgnd_mov_files)):
                mov_file = backgnd_mov_files[i]
                try:
                    indx = self.video_indices[mov_file]
                except:
                    ErrorDlg(self, 'ERROR: could not locate %s in %s.' % (mov_file, self.varStimFirst['nostep']['Video file']))
                    self.bRunning = 0
                    return
                self.backgnd_mov_index_to_long_video_index.append(indx)

            if nPhase not in self.signal_index_list:
                # create list of indexes that will be used to select the SIGNAL WAV and MOV files
                self.signal_index_list[nPhase] = create_shuffled_index_list(n_A_wavs_this_phase)
                self.signal_index_list_position[nPhase] = 0
                self.signal_index_list_size[nPhase] = len(self.signal_index_list[nPhase])

                # create list of indexes that will be used to select the NO-SIGNAL WAV and MOV files
                self.nosignal_index_list[nPhase] = create_shuffled_index_list(self.n_nosignals)
                self.nosignal_index_list_position[nPhase] = 0
                self.nosignal_index_list_size[nPhase] = len(self.nosignal_index_list[nPhase])

                if self.n_probes:
                    # create list of indexes that will be used to select the PROBE WAV and MOV files
                    self.probe_index_list[nPhase] = create_shuffled_index_list(self.n_probes)
                    self.probe_index_list_position[nPhase] = 0
                    self.probe_index_list_size[nPhase] = len(self.probe_index_list[nPhase])

        elif ABX and ABC:
            # open txt files and get long arrays of file names
            backgnd_wav_files = []
            self.n_backgnds = 0 # no repeating background

            max_ans = 2
            if THREE_ALT:
                max_ans = 3
            n_rows,signal_wav_files,B_wav_files,C_wav_files,self.abc_answers = load_abc_wav_filenames(self,
                self.varStimFirst['nostep']['Wav folder'], var['Signal wav files'], max_ans)
            if not n_rows:
                ErrorDlg(self, 'ERROR: reading ABC wav file.')
                self.bRunning = 0
                return
            n_abx_wav_rows_this_phase = n_rows
            n_A_wavs_this_phase = n_rows # always 1 column in ABC
            signal_wav_files.extend(B_wav_files)
            signal_wav_files.extend(C_wav_files)
            self.n_signals = len(signal_wav_files) # 3x instead of 2x !
            if nPhase not in self.signal_index_list:
                # create list of indexes that will be used to select the "A", "B", and "C" wav files
                self.signal_index_list[nPhase] = create_shuffled_index_list(n_A_wavs_this_phase)
                self.signal_index_list_position[nPhase] = 0
                self.signal_index_list_size[nPhase] = len(self.signal_index_list[nPhase])
            if len(self.nSignalByRow[nPhase]) == 0:
                # initialize
                for i in range(n_A_wavs_this_phase):
                    self.nSignalByRow[nPhase][i]=0
                    self.nCorrectSignalByRow[nPhase][i]=0
                    self.latencyByRow[nPhase][i]=0.0
##        elif ABX: # ABX, but not ABC
##            # open txt files and get long arrays of file names
##            backgnd_wav_files = []
##            self.n_backgnds = 0 # no repeating background
##
##            n_cols, n_rows, signal_wav_files = load_wav_filenames(self,
##                self.varStimFirst['nostep']['Wav folder'],
##                var['Signal wav files'])
##            print 'read %d columns and %d rows of signal wav files' % (n_cols, n_rows)
##            n_A_wavs_this_phase = n_cols * n_rows
##            if n_A_wavs_this_phase != len(signal_wav_files):
##                ErrorDlg(self, 'ERROR: number of "A" (signal) wav files is %d, does not match product of number of rows (%d) and columns (%d) ' % (
##                    len(signal_wav_files), n_rows, n_cols))
##                self.abort = 1
##                return
##            n_abx_wav_cols_this_phase, n_abx_wav_rows_this_phase, wav_files = load_wav_filenames(self,
##                self.varStimFirst['nostep']['Wav folder'],
##                var['Background wav files'])
##            print 'read %d columns and %d rows of background wav files' % (n_abx_wav_cols_this_phase, n_abx_wav_rows_this_phase)
##            if n_abx_wav_rows_this_phase != n_rows:
##                ErrorDlg(self, 'ERROR: number of "B" (background) rows of wav files is %d, does not match number of rows in "A" (signal) %d' % (
##                    n_abx_wav_rows_this_phase, n_rows))
##                self.abort = 1
##                return
##            if n_A_wavs_this_phase != len(wav_files):
##                ErrorDlg(self, 'ERROR: number of "B" (background) wav files is %d, does not match product of number of rows (%d) and columns (%d) ' % (
##                    len(wav_files), n_abx_wav_rows_this_phase, n_abx_wav_cols_this_phase))
##                self.abort = 1
##                return
##            signal_wav_files.extend(wav_files)
##            self.n_signals = len(signal_wav_files)
##            # create list of indexes that will be used to select the "A" and "B" wav files AND order (ABA, ABB, BAA, or BAB)
##            self.abx_list = create_abx_list(n_A_wavs_this_phase)
##            self.abx_list_position = 0
##            self.abx_list_size = len(self.abx_list)
##            # print 'new shuffle, 1st 4 items are ',self.abx_list[:4]
##            if len(self.nSignalByRow[nPhase]) == 0:
##                # initialize
##                for i in range(n_abx_wav_rows_this_phase):
##                    self.nSignalByRow[nPhase][i]=0
##                    self.nCorrectSignalByRow[nPhase][i]=0
##                    self.latencyByRow[nPhase][i]=0.0
        elif AB:
            # 2AFC
            backgnd_wav_files = []
            self.n_backgnds = 0 # no repeating background
            if mode == 'UPDOWN' and var['Step wav rows']:
                n_cols, n_rows, signal_wav_files = load_wav_filenames(self,
                    self.varStimFirst['nostep']['Wav folder'],
                    var['Signal wav files'])
                print('read %d columns and %d rows of signal wav files' % (n_cols, n_rows))
                n_A_wavs_this_phase = n_cols
                n_wav_rows_this_phase = n_rows
                n_cols2, n_rows2, wav_files = load_wav_filenames(self,
                    self.varStimFirst['nostep']['Wav folder'],
                    var['Background wav files'])
                print('read %d columns and %d rows of background wav files' % (n_cols2, n_rows2))
            else:
                signal_wav_files = wav_fn_str_to_list(var['Signal wav files'])
                n_A_wavs_this_phase = len(signal_wav_files)
                wav_files = wav_fn_str_to_list(var['Background wav files'])
                if n_A_wavs_this_phase != len(wav_files):
                    ErrorDlg(self, 'ERROR: number of "A" (signal) wav files (%d) not equal to number of "B" (background) wav files (%d).' % (
                        n_A_wavs_this_phase, len(wav_files)))
                    self.abort = 1
                    return
            signal_wav_files.extend(wav_files)
            self.n_signals = len(signal_wav_files)
        else:
            backgnd_wav_files = wav_fn_str_to_list(var['Background wav files'])
            signal_wav_files = wav_fn_str_to_list(var['Signal wav files'])
            self.n_backgnds = len(backgnd_wav_files)
            self.n_signals = len(signal_wav_files)

            if nPhase not in self.signal_index_list:
                # create list of indexes that will be used to select the SIGNAL WAV
                self.signal_index_list[nPhase] = create_shuffled_index_list(self.n_signals)
                self.signal_index_list_position[nPhase] = 0
                self.signal_index_list_size[nPhase] = len(self.signal_index_list[nPhase])

        backgnd2_wav_files = wav_fn_str_to_list(var['Background2 wav files'])
        self.n_backgnd2s = len(backgnd2_wav_files)
        if not MULTI_TRIAL_VIDEO_FILES:# always load, even if not enabled, in case they want to force a probe #catchAndProbeVars['Enable probe trials']:
            probe_wav_files = wav_fn_str_to_list(var['Probe wav files'])
            self.n_probes = len(probe_wav_files)

        stim_dur = float(self.varStimFirst['nostep']['Duration'])
        if SIMULATION:
            ch2_dur = 0.0
        else:
            ch2_dur = longest_wav_dur(self, self.varStimFirst['nostep']['Wav folder'], backgnd2_wav_files)
        stim_gap = float(self.varStimFirst['nostep']['Time between tones'])
        if ABX:
            n_tones_per_sig = 3
        elif AB:
            n_tones_per_sig = 2
        else:
            n_tones_per_sig = int(self.varStimFirst['nostep']['Num tones per trial'])
        tStimDur = stim_dur + (n_tones_per_sig-1)*(stim_dur+stim_gap)

        if mode == 'TRAIN':
            if not self.rand_intensity_list_size[nPhase]:
                # normal
                self.fSignalInten = var['Intensity']
                signal_level_stored = self.fSignalInten # store signal at level we want to play it
            else:
                # random intensities
                # self.fSignalInten will be set later, but give it a value now, just in case
                self.fSignalInten = self.rand_intensity_list[nPhase][self.rand_intensity_list_position[nPhase]]
                signal_level_stored = 0 # store signal at max level
        elif mode == 'UPDOWN':
            if var['Step wav rows']:
                # print 'changing WAV row'
                self.bUpDownIsIntensity = False
                self.iSignalWavRow = int(var['Initial row']) - 1 # 1st is zero
                if self.iSignalWavRow >= n_wav_rows_this_phase:
                    ErrorDlg(self, 'ERROR: your "Initial row" setting is too high. The max it can be is %d (the number of rows).' % n_wav_rows_this_phase)
                    self.abort = 1
                    return
                self.fSignalInten = var['Initial intensity'] # init signal level
                signal_level_stored = self.fSignalInten # store signal at level we want to play it
                self.fSignalStepSize = var['Initial step size']
                bAdjSignalDifficulty = 0
            else:
                # print 'changing intensity'
                self.bUpDownIsIntensity = True
                self.fSignalInten = var['Initial intensity'] # init signal level
                signal_level_stored = 0 # store signal at max level
                self.fSignalStepSize = var['Initial step size']
                bAdjSignalDifficulty = 0
        else:
            ErrorDlg(self, 'UNKNOWN MODE')
            self.abort = 1
            return

        if not SIMULATION:
            #if UPDOWN: atten=0, else level like before
            stat = self.StartSoundThread(var, catchAndProbeVars, signal_level_stored, signal_wav_files, backgnd_wav_files, backgnd2_wav_files,
                                         probe_wav_files, nosignal_wav_files, stim_dur, ch2_dur)
            if stat:
                self.abort = 1
                return
            if REPEATING_VIDEO:
                self.StartVideoThread()

        self.nStepNum = 0

        self.nCorrInARow = self.mMissInARow = self.nDir = self.nReversals = self.nStepsInCurrDir = 0

        if mode=='UPDOWN':
            nReversalsUsed = self.nReversals - var['Ignore']
            self.fThrEst = numpy.zeros(shape=(var['Use'],),dtype=numpy.float32)
        else:
            nReversalsUsed = 0

        self.nLastStepNumDoubled = 0
        self.nLastReversalStepNum = 0

        strSignalHitRate = ''
        strProbeHitRate = ''
        strFalseAlarmRate = ''

        # nMaxTrials - GOTTA BE RIGHT!
        nMaxTrials = var['Max number of trials']
        if mode == 'TRAIN' and var['Max number of trials2'] > nMaxTrials:
            nMaxTrials = var['Max number of trials2']

        self.nTrialType = numpy.zeros(shape=(nMaxTrials,), dtype=numpy.int8)
        self.bCorrect = numpy.zeros(shape=(nMaxTrials,), dtype=numpy.bool_)

        if catchAndProbeVars['Enable No-signal trials']:
            nCatchRatio_catch = catchAndProbeVars['No-sig ratio no-sig']
            nCatchRatio_sig = catchAndProbeVars['No-sig ratio sig']
        else:
            nCatchRatio_catch = 0
            nCatchRatio_sig = 1

        if catchAndProbeVars['Enable probe trials']:
            nProbeRatio_probe = catchAndProbeVars['Probe ratio probe']
            nProbeRatio_sig = catchAndProbeVars['Probe ratio sig']
        else:
            nProbeRatio_probe = 0
            nProbeRatio_sig = 1

        self.InitRandTrials(catchAndProbeVars['Random block size'],
                            catchAndProbeVars['Enable No-signal trials'], catchAndProbeVars['Enable probe trials'],
                            catchAndProbeVars['No-sig ratio no-sig'], catchAndProbeVars['No-sig ratio sig'], 
                            catchAndProbeVars['Probe ratio probe'], catchAndProbeVars['Probe ratio sig'])

        tStimComplete = -1

        nTrial = -1
        bDone = False

        self.controlPanel.currentPhase.SetValue(str(nPhase+1))
        self.controlPanel.trialNumber.SetValue(str(nTrial+1))
        self.controlPanel.trialNumber2.SetValue('')
        self.controlPanel.numReversalsUsed.SetValue(str(nReversalsUsed))

        self.nSignal_current = 0 # used for bounce-back test
        
        self.controlPanel.hitRateTotal.SetValue('')
        self.controlPanel.hitRateLast5.SetValue('')
        self.controlPanel.falseAlarmRateTotal.SetValue('')
        self.controlPanel.falseAlarmRateLast5.SetValue('')
        self.controlPanel.probeHitRateTotal.SetValue('')
        self.controlPanel.probeHitRateLast5.SetValue('')
        fTrialDur = var['Trial duration']
        fTrialDelay = var['Trial delay']

        if EYE_TRACK:
            nTrialsBeforeInterTrialVideo = var['Play video once for every']
            if nTrialsBeforeInterTrialVideo:
                nTrialsBeforeInterTrialVideo += 1 # one more to account where this is handled (top of loop)
                inter_trial_videos = list(all_files(
                    os.path.join(self.varStimFirst['nostep']['Wav folder'],
                                 'inter_trial_videos'),
                    '*.mpg', single_level=True))
                num_inter_trial_videos = len(inter_trial_videos)
                if not num_inter_trial_videos:
                    # disable
                    nTrialsBeforeInterTrialVideo = 0

        self.abort = 0
        if ROBOT_SIM:
            time.sleep(2.0)
        if not SIMULATION and not ROBOT_SIM:
            if ADULT_SUBJECT:
                self.dlg_init_trial.Show()
                MsgDlg(self, 'Ready', 'Hit enter to begin 1st trial')
                self.dlg_init_trial.Show(False)
            else:
                ret = OKCancelDlg(self, 'Ready', 'Hit enter to begin 1st trial, or CANCEL to SKIP to next phase.') # add option to ABORT here
                if ret == wx.ID_CANCEL:
                    # skip
                    self.abort = 2
                    self.outFile.write('SkipToNextPhase selected before 1st trial\n')
                    bDone = True

        timeWillStart = ctypes.c_int(0)
        nMsBeforeStart = ctypes.c_int(0)
        nMsDelay = ctypes.c_int(0)
        bForceProbeTrial = False
        while not bDone:
            self.bInRun = True
            if not SIMULATION:
                print('-----------------')
            nTrial += 1
            nTrial2 = nTrial+self.nTrialsSoFar[nPhase]
            self.nTrialNumAll[self.nTrialIndexAll] = nTrial2
            self.nPhaseAll[self.nTrialIndexAll] = nPhase
            
            nStepNum = self.nStepNum # save for output (self.nStepNum may get updated in the body of this loop)
            self.controlPanel.trialNumber.SetValue(str(nTrial+1))
            self.controlPanel.trialNumber2.SetValue(str(nTrial2+1))

            # may want to move this block? SILENT, WILL WANT TO MOVE SOME OF THIS CODE
            if EYE_TRACK and nTrialsBeforeInterTrialVideo:
                nTrialsBeforeInterTrialVideo -= 1
                if not nTrialsBeforeInterTrialVideo:
                    irnd = random.randint(0, num_inter_trial_videos-1)
                    self.baby_feedback.yippee(0, play_this_instead=inter_trial_videos[irnd])
                    self.baby_feedback.whiteout() # works
                    pygame.mixer.init(self.pygame_srate, -16, 2, self.pygame_out_buff_sz)
                    nTrialsBeforeInterTrialVideo = var['Play video once for every']

            bAdjSignalDifficulty = 0
            strStim = ''
            if not bForceProbeTrial:
                bDone,nRand = self.SelectTrialType(mode, var, catchAndProbeVars, nTrial, nPhase);
                # if bDone, it could be FAIL or PASS
                # FAIL: hit max num trials setting (abort=1) or bounce-back (abort=4)
                # PASS: move on, we met the criteria (stopping rules)
                if bDone:
                    break
            else:
                # special case - force a probe trial
                self.strTrialType = "probe"
                self.fIntenCh1 = catchAndProbeVars['Probe intensity']
                self.nTrialType[nTrial] = pb.PROBE
                self.bIsProbe = 1
                self.nProbe[nPhase] += 1
                self.nTrialTypeAll[self.nTrialIndexAll] = self.nTrialType[nTrial]
                bDone = False
                nRand = 0 # should not be used. zero to be safe

            # wait for stim to finish
            #print '-- begin wait for stim to finish'
            if not SIMULATION:
                while (time.clock() < tStimComplete):
                    # don't just sleep, because I want to respond to keybd, mouse (?)
                    time.sleep(0.1) # FREE UP CPU ? MAKE EVENT DRIVEN?
                    wx.Yield()
            #print '-- end wait for stim to finish'
            #print 'self.sound_thread.isAlive()=',self.sound_thread.isAlive()

            #t1 = time.clock()

            # print 'BEFORE:',self.nTrialType[nTrial]

            # ------ determine signal (or no-sig) -----
            i_buffs = (ctypes.c_int * n_tones_per_sig)() # array to pass

            strStimAtten = '' # the wav file we use calibration for (if >1 tone)

            current_mov = ''
            if self.nTrialType[nTrial] == pb.NO_SIG and not MULTI_TRIAL_VIDEO_FILES:
                # no-signal trial (and not MULTI_TRIAL_VIDEO_FILES, which is handled below)
                sig_typ = 0
                num_wav_files = self.n_backgnds
                if not num_wav_files:
                    # silent background
                    rand_sig = 0
                    strStim = ''
                else:
                    rand_sig = random.randint(0,num_wav_files-1)
                    strStim = backgnd_wav_files[rand_sig] # 1st wav to play
            elif self.bIsProbe and not MULTI_TRIAL_VIDEO_FILES:
                # probe
                sig_typ = 2
                num_wav_files = self.n_probes
                rand_sig = random.randint(0,num_wav_files-1)
                if rand_sig < 0 or rand_sig >= self.n_probes:
                    ErrorDlg(self, 'rand probe out of bounds: %s' % rand_sig)
                    rand_sig = 0
                strStim = probe_wav_files[rand_sig] # 1st wav to play
            else:
                # signal (unless MULTI_TRIAL_VIDEO_FILES, then it could be no-sig or probe)

                # define sig_typ, pick next rand_sig (if using self.signal_index_list[nPhase])
                if self.nTrialType[nTrial] == pb.NO_SIG:
                    # NO-SIGNAL DONE HERE (and below), because of the code in common with SIGNAL
                    sig_typ = 3 # nosig from own buffer
                    if 1:#self.nosignal_index_list_size[nPhase]: # SHOULD ALWAYS BE TRUE, CRASH IF NOT
                        rand_sig = self.nosignal_index_list[nPhase][self.nosignal_index_list_position[nPhase]]
                        self.nosignal_index_list_position[nPhase] += 1
                        if self.nosignal_index_list_position[nPhase] >= self.nosignal_index_list_size[nPhase]:
                            self.nosignal_index_list_position[nPhase] = 0
                            random.shuffle(self.nosignal_index_list[nPhase])
                elif self.bIsProbe:
                    # PROBE DONE HERE (and below), because of the code in common with SIGNAL
                    sig_typ = 2 # probe from own buffer
                    if 1:#self.probe_index_list_size[nPhase]: # SHOULD ALWAYS BE TRUE, CRASH IF NOT
                        rand_sig = self.probe_index_list[nPhase][self.probe_index_list_position[nPhase]]
                        self.probe_index_list_position[nPhase] += 1
                        if self.probe_index_list_position[nPhase] >= self.probe_index_list_size[nPhase]:
                            self.probe_index_list_position[nPhase] = 0
                            random.shuffle(self.probe_index_list[nPhase])
                else:
                    # NORMAL (SIGNAL)
                    sig_typ = 1
                    if self.signal_index_list_size[nPhase]:
                        rand_sig = self.signal_index_list[nPhase][self.signal_index_list_position[nPhase]]
                        self.signal_index_list_position[nPhase] += 1
                        if self.signal_index_list_position[nPhase] >= self.signal_index_list_size[nPhase]:
                            self.signal_index_list_position[nPhase] = 0
                            random.shuffle(self.signal_index_list[nPhase])

                if MULTI_TRIAL_VIDEO_FILES:
                    if REPEATING_VIDEO:
                        t_timeout = time.clock() + g_max_mov_dur + 1.0 # give an extra 1 sec
                        while g_backgnd_video_active:
                            # wait for BG video to end
                            time.sleep(0.02)
                            wx.Yield()
                            if time.clock() > t_timeout:
                                # timeout! something is wrong
                                print('TIMEOUT waiting for g_backgnd_video_active##############')
                                ErrorDlg(self, 'TIMEOUT waiting for g_backgnd_video_active')
                                break

                    if self.nTrialType[nTrial] == pb.NO_SIG:
                        # NO-SIGNAL DONE HERE, because of the code in common with SIGNAL
                        # overwrite strStim
                        num_wav_files = self.n_nosignals
                        strStim = nosignal_wav_files[rand_sig]
                        current_mov = nosignal_mov_files[rand_sig]
                    elif self.bIsProbe:
                        # PROBE DONE HERE, because of the code in common with SIGNAL
                        # overwrite strStim
                        num_wav_files = self.n_probes
                        strStim = probe_wav_files[rand_sig]
                        current_mov = probe_mov_files[rand_sig]
                    else:
                        # NORMAL (SIGNAL)
                        # signal_wav_files, mov_files
                        strStim = signal_wav_files[rand_sig] # wav to play
                        current_mov = mov_files[rand_sig]

                    strStimAtten = strStim # wav file to calibrate to (Mona 2/9/15)

                    if current_mov != 'BLANK.mov':
                        g_foregnd_video_active = True
                        try:
                            i = self.video_indices[current_mov]
                        except:
                            ErrorDlg(self, 'Could not locate %s in CSV file' % current_mov)
                            bDone = 1
                            break
                        #self.video_lengths[i]

                        video_offset = self.video_offsets[i]
                        if REPEATING_VIDEO:
                            stat = analog_io.WhenNextBackgnd(ctypes.pointer(timeWillStart), ctypes.pointer(nMsBeforeStart), ctypes.pointer(nMsDelay))
                            if stat:
                                ErrorDlg(self, 'Error from analog_io.WhenNextBackgnd: %s' % stat)
                                bDone = 1
                                break
                            video_offset_delay_ms = video_offset_jitter_ms - nMsDelay.value
                            video_offset += video_offset_delay_ms

                        # print '%.3f FG about to seek for SIGNAL %s (offset=%s)' % (time.clock(), current_mov, self.video_offsets[i])
                        if video_lock.locked():
                            print('############ LOCKED, WILL NEED TO WAIT #######')
                        with video_lock:
                            g_player.Seek(video_offset) #wx.FromStart

                elif ABX and ABC:
                    #--- begin ABC
                    if THREE_ALT:
                        third_same_as_2nd = self.abc_answers[rand_sig]
                    else:
                        third_same_as_2nd = self.abc_answers[rand_sig] - 1
                    abx_row = rand_sig
                    self.nSignalByRow[nPhase][abx_row] += 1
                    strStim = ''
                    strStimAtten = '' # wav file to calibrate to (Mona 2/9/15)
                    for i in range(3):
                        indx = rand_sig + i*n_A_wavs_this_phase
                        if i != 0:
                            strStim += ','
                        strStim += signal_wav_files[indx]
                        if i+1 == third_same_as_2nd:
                            strStimAtten = signal_wav_files[indx]
                        i_buffs[i] = indx
                        print(i_buffs[i])
                    #--- end ABC
##                elif ABX: # ABX, but not ABC
##                    #--- begin ABX
##                    #B_is_first = random.randint(0,1) # 0:start with A 1:start with B
##                    #third_same_as_2nd = random.randint(0,1) # 0: 3rd same as 1st 1: 3rd same as 2nd
##
##                    rand_sig,B_is_first,third_same_as_2nd = self.abx_list[self.abx_list_position]
##                    abx_row,col = index_to_row_col(rand_sig, n_abx_wav_rows_this_phase, n_abx_wav_cols_this_phase)
##                    print 'about to play row %d col %d' % (abx_row,col)
##                    if abx_row < 0:
##                        print 'OMG: rand_sig, n_abx_wav_rows_this_phase, n_abx_wav_cols_this_phase',rand_sig, n_abx_wav_rows_this_phase, n_abx_wav_cols_this_phase
##                    self.nSignalByRow[nPhase][abx_row] += 1
##                    self.abx_list_position += 1
##                    if self.abx_list_position >= self.abx_list_size:
##                        self.abx_list_position = 0
##                        # random.shuffle(self.abx_list)
##                        self.abx_list = create_abx_list(n_A_wavs_this_phase) # remake instead of shuffle, in case alg does not do simple shuffle
##                        # print 'new shuffle, 1st 4 items are ',self.abx_list[:4]
##
##                    if B_is_first:
##                        i1 = rand_sig + n_A_wavs_this_phase
##                        i2 = rand_sig
##                    else:
##                        i1 = rand_sig
##                        i2 = rand_sig + n_A_wavs_this_phase
##                    if third_same_as_2nd:
##                        i3 = i2
##                    else:
##                        i3 = i1
##                    strStim = signal_wav_files[i1] # 1st wav to play
##                    i_buffs[0] = i1
##                    # 2nd tone (B)
##                    strStim += ',%s' % signal_wav_files[i2]
##                    i_buffs[1] = i2
##                    # 3rd tone (A or B)
##                    strStim += ',%s' % signal_wav_files[i3]
##                    i_buffs[2] = i3
##                    for i in range(3):
##                        print i_buffs[i]
##                    #--- end ABX
                elif AB:
                    # 2AFC
                    # rand_sig not used
                    # n_A_wavs_this_phase = number of A wav files (same as B)
                    B_is_first = random.randint(0,1) # 0:start with A 1:start with B
                    rand_sig_indx_A = random.randint(0,n_A_wavs_this_phase-1)
                    if int(self.varStimFirst['nostep']['Randomize within trial']):
                        # index of B not related to index of A
                        rand_sig_indx_B = random.randint(0,n_A_wavs_this_phase-1)
                    else:
                        rand_sig_indx_B = rand_sig_indx_A

                    if mode == 'UPDOWN' and not self.bUpDownIsIntensity:
                        # up/down using wav rows
                        rand_sig_indx_A += self.iSignalWavRow*n_A_wavs_this_phase
                        rand_sig_indx_B += self.iSignalWavRow*n_A_wavs_this_phase
                        if B_is_first:
                            i1 = rand_sig_indx_B + self.n_signals/2
                            i2 = rand_sig_indx_A
                        else:
                            i1 = rand_sig_indx_A
                            i2 = rand_sig_indx_B + self.n_signals/2
                        print('AB iSignalWavRow=', (self.iSignalWavRow+1))
                    else:
                        # non-adaptive or (updown and intensity)
                        if B_is_first:
                            i1 = rand_sig_indx_B + n_A_wavs_this_phase
                            i2 = rand_sig_indx_A
                        else:
                            i1 = rand_sig_indx_A
                            i2 = rand_sig_indx_B + n_A_wavs_this_phase
                        if mode == 'UPDOWN' and self.bUpDownIsIntensity or self.rand_intensity_list_size[nPhase]:
                            # A is signal, the wav we are altering intensity
                            strStimAtten = signal_wav_files[rand_sig_indx_A]
                    strStim = signal_wav_files[i1] # 1st wav to play
                    i_buffs[0] = i1
                    # 2nd tone (B)
                    strStim += ',%s' % signal_wav_files[i2]
                    i_buffs[1] = i2
                    for i in range(2):
                        print(i_buffs[i])
                else:
                    #--- begin NOT (MULTI_TRIAL_VIDEO_FILES ABX AB)
                    num_wav_files = self.n_signals # used for range of random numbers
                    # rand_sig = nRand - self.nProbeThresh - 1 # the first random signal to play (DONE ABOVE NOW)
                    if rand_sig < 0 or rand_sig >= num_wav_files:
                        code = '%s-%s-%s-%s-%s-%s-%s-%s' % (self.nBins, catchAndProbeVars['Random block size'],
                                                            catchAndProbeVars['Enable No-signal trials'],
                                     catchAndProbeVars['Enable probe trials'], catchAndProbeVars['No-sig ratio no-sig'],
                                     catchAndProbeVars['No-sig ratio sig'], catchAndProbeVars['Probe ratio probe'],
                                     catchAndProbeVars['Probe ratio sig'])

                        ErrorDlg(self, 'rand_sig out of bounds: rand_sig=%s, nProbeThresh=%s, n_signals=%s, CODE:%s' % (rand_sig,self.nProbeThresh,self.n_signals,code))

                        rand_sig = 0
                    strStim = signal_wav_files[rand_sig] # 1st wav to play
                    #--- end NOT ABX
                    
            if not ABX and not AB:
                self.strTrialType += str(rand_sig+1)
                i_buffs[0] = rand_sig
                for i in range(1, n_tones_per_sig):
                    if int(self.varStimFirst['nostep']['Randomize within trial']):
                        if not (self.nTrialType[nTrial] == pb.NO_SIG and self.n_backgnds == 0):
                            rand_sig = random.randint(0,num_wav_files-1)
                    i_buffs[i] = rand_sig
                    if sig_typ == 1:
                        strStim += ',%s' % signal_wav_files[rand_sig]
                    elif sig_typ == 2:
                        strStim += ',%s' % probe_wav_files[rand_sig]
                    elif self.n_backgnds:
                        strStim += ',%s' % backgnd_wav_files[rand_sig]

            if SIMULATION:
                bGotResponse = 0
                respLatency = -1
                if AB or ABX:
                    # there are no sig/no-sig 
                    if random.random() <= self.sim_sig_resp_prob:
                        bGotResponse = 1
                        respLatency = 0
                else:
                    if self.nTrialType[nTrial] == pb.NO_SIG:
                        # no-signal
                        if random.random() <= self.sim_nosig_resp_prob:
                            bGotResponse = 1
                            respLatency = 0
                    elif self.bIsProbe:
                        # probe
                        if random.random() <= self.sim_probe_resp_prob:
                            bGotResponse = 1
                            respLatency = 0
                    else:
                        # signal
                        if random.random() <= self.sim_sig_resp_prob:
                            bGotResponse = 1
                            respLatency = 0
            else:
                # --- begin non-SIMULATION block
                output_period_ms = int(0.5 + 1e3 * self.buff_size / float(self.sample_rate))
                if EYE_TRACK:
                    # DELAY SHOULD BE HERE, so that after grow_shrink_dot we go to colored squares right away
                    stat = analog_io.WhenNextBackgnd(ctypes.pointer(timeWillStart), ctypes.pointer(nMsBeforeStart), ctypes.pointer(nMsDelay))
                    if stat:
                        ErrorDlg(self, 'Error from analog_io.WhenNextBackgnd: %s' % stat)
                        bDone = 1
                        break
                    # "movie" in this section is the orienting stim (grow_shrink_dot). Used word "movie" because code is the same as that used for movie
                    movie_to_sound_delay_ms = int(100+1e3*ORIENTING_STIM_DUR) # 2100 # s/b 2000, is about 2006 on my computer. longer is safer. 2100 will give just under 100ms of delay after grow_shrink_dot
                    movie_head_start_ms = movie_to_sound_delay_ms % output_period_ms # from movie start until next out buff
                    # wait until movie_head_start_ms ms before next
                    if nMsBeforeStart.value > movie_head_start_ms:
                        msToWait = nMsBeforeStart.value - movie_head_start_ms
                    else:
                        msToWait = nMsBeforeStart.value + (output_period_ms-movie_head_start_ms)
                    time.sleep(1e-3*msToWait)

                    self.baby_feedback.grow_shrink_dot()
                    # DELAY WAS HERE
                elif USE_TRIAL_VIDEO and (not MULTI_TRIAL_VIDEO_FILES or current_mov != 'BLANK.mov'):
                    #self.movie_frame.Show(True) # always visible now
                    stat = analog_io.WhenNextBackgnd(ctypes.pointer(timeWillStart), ctypes.pointer(nMsBeforeStart), ctypes.pointer(nMsDelay))
                    if stat:
                        ErrorDlg(self, 'Error from analog_io.WhenNextBackgnd: %s' % stat)
                        bDone = 1
                        break
                    video_offset_delay_ms2 = video_offset_jitter_ms - nMsDelay.value
##                    if ROBOT_SIM:
##                        print 'video_offset_jitter_ms=%s nMsDelay=%s video_offset_delay_ms2=%s video_offset_delay_ms=%s' % (
##                            video_offset_jitter_ms, nMsDelay.value, video_offset_delay_ms2, video_offset_delay_ms)
                    if video_offset_delay_ms != video_offset_delay_ms2:
                        ErrorDlg(self, 'ERROR - nMsDelay has changed, please tell Brandon')

                    movie_to_sound_delay_ms = int(1e3*self.varStimFirst['nostep']['Video sound delay']) # sb 2000
                    movie_head_start_ms = movie_to_sound_delay_ms % output_period_ms # from movie start until next out buff
                    
                    # 1. wait until movie_head_start_ms ms before next
                    if nMsBeforeStart.value > movie_head_start_ms:
                        msToWait = nMsBeforeStart.value - movie_head_start_ms
                    else:
                        msToWait = nMsBeforeStart.value + (output_period_ms-movie_head_start_ms)
                    msToWait += video_offset_delay_ms
                    time.sleep(1e-3*msToWait)
                    # 2. start movie
                    if MULTI_TRIAL_VIDEO_FILES:
                        try:
                            video_length = 1e-3 * self.video_lengths[self.video_indices[current_mov]]
                        except KeyError:
                            ErrorDlg(self, 'ERROR: could not locate %s in CSV file.' % current_mov)
                        # print '%.3f FG about to PLAY SIG (length = %.1f sec)' % (time.clock(), video_length)
                    else:
                        if video_lock.locked():
                            print('############ LOCKED, WILL NEED TO WAIT #######')
                        with video_lock:
                            video_length = 1e-3 * self.movie_frame.player.Length()
                        print('video_length=',video_length)
                        if not video_length:
                            # ErrorDlg(self, 'Error: video seems bad (zero length)')
                            video_length = 2.0 #  0.2 truncates 5.0 makes me wait a long time before I can answer
                            print('##### video appears to have zero length! This problem comes and goes. Will now set to %s seconds' % video_length)
                            #bDone = 1
                            #break
                    if video_lock.locked():
                        print('############ LOCKED, WILL NEED TO WAIT #######')
                    with video_lock:
                        self.movie_frame.player.Play()
                    self.movie_timer.Start(video_length*1e3, oneShot=True) # arg in ms. tell me when 0 sec left
                    time_movie_end = time.clock() + video_length + 0.1 # wait an extra 100 ms to be safe
                    # 3. wait output_period_ms/2 + output_period_ms + movie_head_start_ms
                   #msToWait = output_period_ms/2 + output_period_ms + movie_head_start_ms # before 6/25/2014
                    msToWait = movie_to_sound_delay_ms - output_period_ms/2 # 6/25/2014
                    if msToWait < 0:
                        pass
                        #print 'Error: Your "Video sound delay" is too short by about %d ms' % -msToWait
##                        bDone = 1
##                        break
                    else:
                        time.sleep(1e-3*msToWait)
                    # 4. Signal

                # if UPDOWN (or using random intensities) set atten here
                if sig_typ == 1 and (mode == 'UPDOWN' and self.bUpDownIsIntensity or self.rand_intensity_list_size[nPhase]):
                    # signal

                    if self.rand_intensity_list_size[nPhase]:
                        # random intensity
                        self.fSignalInten = self.rand_intensity_list[nPhase][self.rand_intensity_list_position[nPhase]]
                        self.rand_intensity_list_position[nPhase] += 1
                        if self.rand_intensity_list_position[nPhase] >= self.rand_intensity_list_size[nPhase]:
                            self.rand_intensity_list_position[nPhase] = 0
                            random.shuffle(self.rand_intensity_list[nPhase])

                    if not strStimAtten:
                        strStimAtten = strStim
                    atten = self.max_dB_SPL[0][strStimAtten] - self.fSignalInten # signal on chan 0
                    if atten < 0.0:
                        WarnDlg(self,
                                'Cannot get to %.1f dB SPL with %s on chan %s. The highest I can reach is %.1f dB SPL.' % (
                                    self.fSignalInten, strStim, 1, self.max_dB_SPL[0][strStim]))
                        atten = 0.0
                    atten_times100 = int(atten*100.0)
                    analog_io.SetAttenFactor(0, atten_times100)
                # print 'sig_typ={}, n_tones_per_sig={}, i_buffs={}'.format(sig_typ, n_tones_per_sig, [i_buffs[i] for i in range(n_tones_per_sig)])
                stat = analog_io.Signal(sig_typ, n_tones_per_sig, ctypes.pointer(i_buffs),
                                        ctypes.pointer(timeWillStart), ctypes.pointer(nMsBeforeStart), ctypes.pointer(nMsDelay) )
                if stat:
                    ErrorDlg(self, 'Error from analog_io.Signal: %s' % stat)
                    bDone = 1
                    break
                # print '%.3f FG after Signal: next_index=%d' % (time.clock(), ctypes.c_int.in_dll(analog_io, "g_backgnd_index").value)

                msToWait = nMsBeforeStart.value
                if msToWait >  nMsDelay.value:
                    msToWait -= nMsDelay.value # should not need to do this!!!??????????
                else:
                    print('MSTOWAIT TOO DAMN SHORT!!')
                #print 'msToWait=',msToWait
                #trial_start_time = t2 + msToWait*1e3

                # wait until begin of trial
                time.sleep(1e-3*msToWait)
                t2 = time.clock()
                # print '%.3f FG backgnd/signal should begin NOW, next_index=%d' % (time.clock(), ctypes.c_int.in_dll(analog_io, "g_backgnd_index").value)
                # print '%.3f FG backgnd/signal should begin NOW, nMsDelay=%d' % (time.clock(), nMsDelay.value)

                if ABX:
                    # wait until 3rd tone begins
                    t_begin_3rd_tone = t2 + 2.0*(stim_dur+stim_gap)
                elif AB:
                    t_begin_2nd_tone = t2 + (stim_dur+stim_gap)

                time_to_hide_dlg_operator = 0
                if EYE_TRACK: # AB
                    self.baby_feedback.show_square(0) #----------------
                    wx.Yield()

                    nice_sleep(t2+stim_dur)

                    self.baby_feedback.whiteout() #-----------------------------
                    wx.Yield()

                    nice_sleep(t2+stim_dur+stim_gap)

                    self.baby_feedback.show_square(1) #---------------------
                    wx.Yield()

                    nice_sleep(t2+stim_dur+stim_gap+stim_dur)

                    self.baby_feedback.whiteout() #-----------------------------
                    wx.Yield()

                elif ADULT_SUBJECT:
                    self.dlg_operator.SetTitle('1')
                    self.dlg_operator.Show()
                    self.dlg_feedback.SetTitle('1')
                    self.dlg_feedback.Show()

                    nice_sleep(t2+stim_dur)

                    self.dlg_operator.Show(False)
                    self.dlg_feedback.Show(False)

                    nice_sleep(t2+stim_dur+stim_gap)

                    self.dlg_operator.SetTitle('2')
                    self.dlg_operator.Show()
                    self.dlg_feedback.SetTitle('2')
                    self.dlg_feedback.Show()

                    nice_sleep(t2+stim_dur+stim_gap+stim_dur)

                    self.dlg_operator.Show(False)
                    self.dlg_feedback.Show(False)
                    if THREE_ALT:
                        nice_sleep(t2+stim_dur+stim_gap+stim_dur+stim_gap)

                        self.dlg_operator.SetTitle('3')
                        self.dlg_operator.Show()
                        self.dlg_feedback.SetTitle('3')
                        self.dlg_feedback.Show()

                        nice_sleep(t2+stim_dur+stim_gap+stim_dur+stim_gap+stim_dur)

                        self.dlg_operator.Show(False)
                        self.dlg_feedback.Show(False)
                else:
                    # we always want some kind of indication during the signal/no-signal
                    self.dlg_operator.SetTitle('')
                    self.dlg_operator.SetBackgroundColour(wx.Colour(red=255))
                    self.dlg_operator.Show()
                    time_to_hide_dlg_operator = t2 + stim_dur

                # beep for user here
                if self.nTrialType[nTrial] != pb.NO_SIG:
                    tStimComplete = t2 + tStimDur
                else:
                    tStimComplete = t2
                #print 'about to wait, fTrialDur=',fTrialDur

                if AB:
                    # wait until 2nd tone begins
                    t_to_sleep = t_begin_2nd_tone-time.clock()
                    if t_to_sleep > 0:
                        time.sleep(t_to_sleep)
                    t2 = t_begin_2nd_tone # time.clock()
                    # ask #HERE# from 2AFC Baby
                    self.controlPanel.init_bar(fTrialDur) #--------------------------------
                    self.ForcedChoiceVars['Choice'] = 0
                    timeout = False
                    while self.ForcedChoiceVars['Choice'] < 1 or self.ForcedChoiceVars['Choice'] > 2:
                        retVal = self.ForcedChoiceDlg(xoffset=-200) # place about 2 inches to left of center
                        if retVal == 'Timeout':
                            timeout = True
                            break
                        if retVal == "Abort":
                            if self.bToyOn:
                                self.TurnOffToy()
                            self.outFile.write('Aborted\n')
                            self.abort = 1
                            break
                    respLatency = self.controlPanel.bar_time
                    # bGotResponse means correct
                    reversed_choice = 0
                    if self.abort or timeout:
                        bGotResponse = False
                    else:
                        # left and right are reversed, so swap 1 and 2
                        if self.ForcedChoiceVars['Choice'] == 1:
                            reversed_choice = 2
                        elif self.ForcedChoiceVars['Choice'] == 2:
                            reversed_choice = 1
                        bGotResponse = B_is_first+1 == reversed_choice # True = correct

                    # tell if correct right away
                    if not self.abort:
                        if not timeout:
                            if bGotResponse:
                                feedback_title = 'correct'
                                feedback_str = '%.0f is correct' % self.ForcedChoiceVars['Choice']
                                set_to_red = False
                            else:
                                feedback_title = 'incorrect'
                                feedback_str = '%.0f is incorrect' % self.ForcedChoiceVars['Choice']
                                set_to_red = True
                        else:
                            feedback_title = 'Bad trial'
                            feedback_str = feedback_title
                            set_to_red = True
                            self.outFile.write('Bad trial\n')
                            # don't count this trial
                            if self.strTrialType == "no-sig":
                                self.nCatch[nPhase] -= 1
                            elif self.strTrialType == "probe":
                                self.nProbe[nPhase] -= 1
                            else:
                                self.nSignal[nPhase] -= 1
                                self.nSignal_current -= 1
                        self.dlg_operator.SetTitle(feedback_str)
                        if set_to_red:
                            self.dlg_operator.SetBackgroundColour(wx.RED)
                        else:
                            self.dlg_operator.SetBackgroundColour(wx.GREEN)
                        self.dlg_operator.Show()

                    # set attenuators if external
                    if hw.devAtten == 'TDT_PA5':
                        if tdt_sys3.SetPA5(self, 0, self.baby_feedback.movie_atten):
                            ErrorDlg(self, 'error setting PA5-1')
                            self.abort = 1
                            return
                    # print 'before MOVIE'
                    self.baby_feedback.yippee(B_is_first)#sigNum) # -------------------------------------------
                    # print 'MOVIE is done'
                    self.baby_feedback.whiteout() # works
                    pygame.mixer.init(self.pygame_srate, -16, 2, self.pygame_out_buff_sz)

                    if not self.abort:
                        self.dlg_operator.Show(False)
                        ret_val = self.OKAbortIECDlg(feedback_title, feedback_str, set_to_red)
                        if ret_val == "Abort":
                            self.outFile.write('Aborted\n')
                            self.abort = 1
                            if timeout:
                                # nothing to do
                                break
                        elif ret_val == "Meas IEC":
                            stat = self.StopSoundThread()
                            if stat:
                                self.outFile.write('Error stopping sound thread. Abort.\n')
                                self.abort = 1
                                break
                            if REPEATING_VIDEO:
                                self.StopVideoThread()

                            self.meas_iec(0)

                            stat = self.StartSoundThread(var, catchAndProbeVars, self.fSignalInten, signal_wav_files,
                                                         backgnd_wav_files, backgnd2_wav_files, probe_wav_files, nosignal_wav_files, stim_dur, ch2_dur)
                            if stat:
                                self.outFile.write('Error starting sound thread. Abort.\n')
                                self.abort = 1
                                break
                            if REPEATING_VIDEO:
                                self.StartVideoThread()
                            InfoDlg(self, 'Hit enter to begin next trial.')
                    if timeout:
                        nTrial -= 1 # cancel the += 1 that comes next
                        continue

                elif ABX:
                    # wait until 3rd tone begins
                    # nice_sleep(t_begin_3rd_tone) - use time.sleep(), to minimize any error due to .1 sec sleep inside wait loop
                    t_to_sleep = t_begin_3rd_tone-time.clock()
                    if THREE_ALT:
                        # ask after 3rd tone played
                        t_to_sleep = t_begin_3rd_tone+stim_dur - time.clock()
                    if t_to_sleep > 0:
                        time.sleep(t_to_sleep)
                    t2 = t_begin_3rd_tone # time.clock()
                    if THREE_ALT:
                        t2 += stim_dur
                    # ask
                    #self.controlPanel.init_bar(fTrialDur)
                    self.ForcedChoiceVars['Choice'] = 0
                    if ROBOT_SIM:
                        # make it correct
                        if THREE_ALT:
                            self.ForcedChoiceVars['Choice'] = third_same_as_2nd
                        else:
                            self.ForcedChoiceVars['Choice'] = third_same_as_2nd+1
                    elif THREE_ALT:
                        while self.ForcedChoiceVars['Choice'] < 1 or self.ForcedChoiceVars['Choice'] > 3:
                            self.bInFCDlg = True
                            self.FCDlg_key_pressed = ''
                            retVal = self.ForcedChoiceDlg(xoffset=-200,
                                                          title='1, 2, or 3',
                                                          prompt = 'Which sound was the target? (Enter 1, 2, or 3)',
                                                          bInFCDlg=self.bInFCDlg) # place about 2 inches to left of center
                            self.bInFCDlg = False
                            if retVal == "Abort":
                                if self.bToyOn:
                                    self.TurnOffToy()
                                self.outFile.write('Aborted.\n')
                                self.abort = 1
                                break
                    else:
                        while self.ForcedChoiceVars['Choice'] < 1 or self.ForcedChoiceVars['Choice'] > 2:
                            self.bInFCDlg = True
                            self.FCDlg_key_pressed = ''
                            retVal = self.ForcedChoiceDlg(xoffset=-200,
                                                          title='1 or 3',
                                                          prompt = 'Is 3rd sound the same as the 1st or 2nd sound? (Enter 1 or 2)',
                                                          bInFCDlg=self.bInFCDlg) # place about 2 inches to left of center
                            self.bInFCDlg = False
                            if retVal == "Abort":
                                if self.bToyOn:
                                    self.TurnOffToy()
                                self.outFile.write('Aborted.\n')
                                self.abort = 1
                                break
                    if self.abort:# or timeout:
                        bGotResponse = 0
                        respLatency = -1
                        break
                    else:
                        t3 = time.clock()
                        respLatency = t3 - t2
                        if THREE_ALT:
                            bGotResponse = third_same_as_2nd == self.ForcedChoiceVars['Choice'] # True = correct
                        else:
                            bGotResponse = third_same_as_2nd+1 == self.ForcedChoiceVars['Choice'] # True = correct
                        print('bGotResponse =',bGotResponse)
                        if ADULT_SUBJECT and var['Give feedback']:
                            if bGotResponse:
                                s = '%.0f is correct' % self.ForcedChoiceVars['Choice']
                            else:
                                s = '%.0f is incorrect' % self.ForcedChoiceVars['Choice']
                            # 
                            self.dlg_feedback.SetTitle(s)
                            self.dlg_feedback.Show()
                            #MsgDlg(self, 'Info', s)
                            time.sleep(2.0)
                            self.dlg_feedback.Show(False)
                elif FORCED_CHOICE: # could have used SYLLABLE 
                    nice_sleep(time_to_hide_dlg_operator)
                    self.dlg_operator.Show(False)
                    time_to_hide_dlg_operator = 0
                else:
                    if ROBOT_SIM:
                        # sleep for duration of stim, then hide the red square
                        nice_sleep(time_to_hide_dlg_operator)
                        self.dlg_operator.Show(False)
                        time_to_hide_dlg_operator = 0
                        nice_sleep(t2+fTrialDelay+fTrialDur/4.0)
                        t3 = -1 # only used below for toy duration
                    else:
                        t3 = self.WaitForUser(t2, fTrialDur, fTrialDelay, time_to_hide_dlg_operator)
                        wx.Yield()
                        if self.abort:
                            self.outFile.write('Aborted.\n')
                            break
                        if t3 >= 0 and (t3-t2) <= fTrialDur:
                            bGotResponse = 1
                            respLatency = t3 - t2
                            # print 'respLatency (before adj for trial delay) = ',respLatency,'DLL lat=',self.tPlusKey2-timeWillStart.value
                            # print 'RAW respLatency = %.3f DLL lat in ms = %d' % (respLatency,self.tPlusKey2-timeWillStart.value)
                            respLatency -= fTrialDelay
                            if respLatency < 0:
                                print('**** respLatency < 0! It is ', respLatency)
                                respLatency = 0.0
                        else:
                            bGotResponse = 0
                            respLatency = -1

                if USE_TRIAL_VIDEO and (not MULTI_TRIAL_VIDEO_FILES or current_mov != 'BLANK.mov'):
                    # wait for video to end
                    while time.clock() < time_movie_end:
                        # print 'wait for movie to end, at %s ms' % self.movie_frame.player.Tell()
                        wx.Yield()
                        time.sleep(0.1)
                    if self.abort:
                        self.outFile.write('Aborted.\n')
                        break
##                    if self.movie_frame.player.Tell() > 0:
##                        print '#### warning: movie seems to still be playing!###'
                if SYLLABLE:
                    SyllableVars['Syllable'] = syllables # options
                    SyllableVars['Other'] = ''
                    dlg = ModelessDlg(self, 'Choose a syllable',
                                      'Choose a syllable',
                                      ['Next', ],
                                      SyllableParams, SyllableVars)

                    if self.dlg_mask1a:
                        # 2nd monitor present
                        w,h = dlg.Size
                        x = self.center2H - w/2
                        y = self.center2V - h/2
                        dlg.SetPosition((x,y))
                    else:
                        dlg.CenterOnScreen()
                    dlg.Show()
                    while not dlg.sButt == 'Next':
                        wx.Yield()
                        dlg.GetData() # will write into dlg.SyllableVars, which, because pass by ref, is our copy
                    print("SyllableVars['Syllable'] = ",SyllableVars['Syllable'])
                    print("SyllableVars['Other'] = ",SyllableVars['Other'])
                    dlg.Destroy()
                    
                    bGotResponse = 1
                    respLatency = 0.0
                should_respond = self.nTrialType[nTrial] != pb.NO_SIG # signal or probe or forced-sig present
                if catchAndProbeVars['Treat probes as no-signals'] and self.bIsProbe:
                    # exception: we want to treat this probe as no-sig
                    should_respond = False
                if ROBOT_SIM:
                    # for the first version - always be right. later i can get from a list.
                    if should_respond:
                        bGotResponse = 1
                        respLatency = 0.5
                    else:
                        bGotResponse = 0
                        respLatency = -1
                if should_respond and ( # signal MUST be present AND
                   bGotResponse or (                          # have response OR ..
                     mode == 'TRAIN' and not var['Toy requires response'])):
                    if hw.TOY_CONTROLLER:
                        # turn on toy (1 of 2)
                        if t3 < 0:
                            # no user response
                            toy_dur = var['Min toy duration']
                        else:
                            toy_dur = var['Trial duration'] - (t3 - t2)
                            if toy_dur < var['Min toy duration']:
                                toy_dur = var['Min toy duration']
                    #if hw.TOY_CONTROLLER:
                        self.TurnOnAToy(toy_dur) # need to turn off toy!!
                # --- end non-SIMULATION block

            if self.bIsProbe and bGotResponse:
                self.nProbeResponses[nPhase] += 1
            should_respond = self.nTrialType[nTrial] != pb.NO_SIG # signal or probe or forced-sig present
            if catchAndProbeVars['Treat probes as no-signals'] and self.bIsProbe:
                # exception: we want to treat this probe as no-sig
                should_respond = False
            if should_respond:
                # signal or probe (not treated as no-sig) or forced-sig present
                if bGotResponse:
                    # correct
                    if self.bIsProbe:
                        pass
                        #self.nProbeResponses[nPhase] += 1
                    else:
                        self.nCorrectSignal[nPhase] += 1
                        if ABX:
                            self.nCorrectSignalByRow[nPhase][abx_row] += 1
                            self.latencyByRow[nPhase][abx_row] += respLatency
                        # signal correct
                        if mode=='UPDOWN':
                            # up/down code
                            self.nCorrInARow += 1
                            self.mMissInARow = 0
                            if self.nCorrInARow >= var['Correct']:
                                # decrease level using PEST rules
                                if self.bUpDownIsIntensity:
                                    self.fLastLevelNCorrect = self.fSignalInten
                                else:
                                    self.fLastLevelNCorrect = self.iSignalWavRow + 1 # 1st is 1
                                self.nCorrInARow = 0
                                bDone = self.AdjStepSizeUsingPEST(-1, var)
                                #if (done) break;
                                nReversalsUsed = self.nReversals - var['Ignore']   # so that display is updated
                                bAdjSignalDifficulty = 1
                    self.bCorrect[nTrial] = True
                else:
                    # not correct
                    if not self.bIsProbe:
                        # signal missed
                        if mode=='UPDOWN':
                            # up/down code
                            self.nCorrInARow = 0
                            self.mMissInARow += 1
                            if self.mMissInARow >= var['Incorrect']:
                                # increase level using PEST
                                if self.bUpDownIsIntensity:
                                    self.fLastLevelMMiss = self.fSignalInten
                                else:
                                    self.fLastLevelMMiss = self.iSignalWavRow + 1 # 1st row is 1
                                self.mMissInARow = 0
                                bDone = self.AdjStepSizeUsingPEST(1, var)
                                #if (done) break;
                                nReversalsUsed = self.nReversals - var['Ignore']   # so that display is updated
                                bAdjSignalDifficulty = 1
                    self.bCorrect[nTrial] = False
                self.bCorrectAll[self.nTrialIndexAll] = self.bCorrect[nTrial]
                self.controlPanel.numReversalsUsed.SetValue(str(nReversalsUsed))
                if self.bIsProbe:
                    strProbeHitRate = "%.0f %%" % (100.0*float(self.nProbeResponses[nPhase])/self.nProbe[nPhase])
                    self.controlPanel.probeHitRateTotal.SetValue(strProbeHitRate)
                    self.controlPanel.probeHitRateLast5.SetValue(self.CalcRecentHitRate(5, pb.PROBE, catchAndProbeVars['Treat probes as no-signals']))
                else:
                    strSignalHitRate = "%.0f %%" % (100.0*float(self.nCorrectSignal[nPhase])/self.nSignal[nPhase])
                    self.controlPanel.hitRateTotal.SetValue(strSignalHitRate)
                    self.controlPanel.hitRateLast5.SetValue(self.CalcRecentHitRate(5, pb.SIGNAL))
                if SIMULATION or ROBOT_SIM or FORCED_CHOICE:
                    retVal = 'OK'
                else:
                    # --- begin NOT SIMULATION block -----
                    if bGotResponse:
                        # correct
                        if self.bIsProbe:
                            retVal = self.EndTrialDlg(True, "CORRECT PROBE. Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                        else:
                            retVal = self.EndTrialDlg(True, "CORRECT SIGNAL. Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                    else:
                        # not correct
                        if self.bIsProbe:
                            retVal = self.EndTrialDlg(False, "INCORRECT PROBE. Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                        else:
                            retVal = self.EndTrialDlg(False, "INCORRECT SIGNAL. Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                    # --- end NOT SIMULATION block -----
            else:
                # signal not present (could be probe that is to be treated as no-sig)
                if not bGotResponse:
                    # correct
                    self.bCorrect[nTrial] = True
                else:
                    # not correct
                    self.bCorrect[nTrial] = False
                    if self.bIsProbe:
                        if not catchAndProbeVars['Treat probes as no-signals']:
                            print('******* IMPOSSIBLE: should_respond=F, bGotResponse=T, self.bIsProbe=T, "Treat probes as no-signals"=F')
                    else:
                        self.nIncorrectCatch[nPhase] += 1
                self.bCorrectAll[self.nTrialIndexAll] = self.bCorrect[nTrial]
                if self.bIsProbe:
                    # probe that is treated as no-sig
                    strProbeHitRate = "%.0f %%" % (100.0*float(self.nProbeResponses[nPhase])/self.nProbe[nPhase]) # still show as hit rate
                    self.controlPanel.probeHitRateTotal.SetValue(strProbeHitRate)
                    self.controlPanel.probeHitRateLast5.SetValue(self.CalcRecentHitRate(5, pb.PROBE, catchAndProbeVars['Treat probes as no-signals']))
                else:
                    strFalseAlarmRate = "%.0f %%" % ( 100.0*float(self.nIncorrectCatch[nPhase])/self.nCatch[nPhase] )
                    self.controlPanel.falseAlarmRateTotal.SetValue(strFalseAlarmRate)
                    self.controlPanel.falseAlarmRateLast5.SetValue(self.CalcRecentHitRate(5, pb.NO_SIG))
                if SIMULATION or ROBOT_SIM or FORCED_CHOICE:
                    retVal = 'OK'
                else:
                    # --- begin NOT SIMULATION block -----
                    if not bGotResponse:
                        # correct
                        if self.bIsProbe:
                            retVal = self.EndTrialDlg(True, "CORRECT PROBE (like NO-SIGNAL). Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                        else:
                            retVal = self.EndTrialDlg(True, "CORRECT NO-SIGNAL. Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                    else:
                        # not correct
                        if self.bIsProbe:
                            retVal = self.EndTrialDlg(False, "INCORRECT PROBE (like NO-SIGNAL). Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                        else:
                            retVal = self.EndTrialDlg(False, "INCORRECT NO-SIGNAL. Hit enter to begin next trial.", END_TRIAL_DLG_X_OFFSET, self.n_probes) # 1st arg is bCorrect
                    # --- end NOT SIMULATION block -----
            # just got back from dlog box (e.g. "CORRECT SIGNAL. Hit enter to begin next trial")
            self.nTrialIndexAll += 1
            if retVal == "Abort":
                if hw.TOY_CONTROLLER and self.bToyOn:
                    self.TurnOffToy()
                self.outFile.write('Aborted.\n')
                self.abort = 1
            if not SIMULATION:
                while hw.TOY_CONTROLLER and self.bToyOn:
                    secsOfToyLeft = self.timeEndToy - time.clock()
                    if ROBOT_SIM:
                        time.sleep(secsOfToyLeft+0.1)
                        secsOfToyLeft = self.timeEndToy - time.clock()
                    s = "The toy will be active for another %.1f seconds. Hit enter to continue." % secsOfToyLeft
                    rv = self.ToyStillActiveDlg(s)
                    if rv == "Abort":
                        self.TurnOffToy()
                        self.abort = 1
                        self.outFile.write('Aborted.\n')
                        retVal = "Abort"
                        break
            if retVal == "Meas IEC":
                stat = self.StopSoundThread()
                if stat:
                    self.outFile.write('Error stopping sound thread.\n')
                    self.abort = 1
                    break
                if REPEATING_VIDEO:
                    self.StopVideoThread()

                self.meas_iec(0)

                stat = self.StartSoundThread(var, catchAndProbeVars, self.fSignalInten, signal_wav_files, backgnd_wav_files, backgnd2_wav_files,
                                             probe_wav_files, nosignal_wav_files, stim_dur, ch2_dur)
                if stat:
                    self.outFile.write('Error starting sound thread.\n')
                    self.abort = 1
                    return
                if REPEATING_VIDEO:
                    self.StartVideoThread()
                InfoDlg(self, 'Hit enter to begin next trial.')
            bForceProbeTrial = retVal == "Force probe trial"

            if not self.simulation_output_only:
                # write results to file

                s = ''
                if MULTI_TRIAL_VIDEO_FILES:
                    # subj #\tphase\t
                    s += "%s\t%s\t" % (
                        self.controlPanel.subjectNumber.GetValue(),
                        self.controlPanel.phaseName[nPhase].GetValue(),
                        )

                # trial\ttrial2\tstep#\tlevel\t
                s += "%d\t%d\t%d\t%.1f\t" % (
                    nTrial+1, nTrial2+1, nStepNum, self.fSignalInten,)

                if not SYLLABLE:
                    # type\tresp_lat_ms\tcorrect\t
                    if self.bCorrect[nTrial]:
                        strIsCorrect = 'Y'
                    else:
                        strIsCorrect = 'N'
                    s += "%s\t%s\t%s\t" % (self.strTrialType,
                                   int(0.5+respLatency*1000),
                                   strIsCorrect, )
                s += "%s\t" % strStim
                if MULTI_TRIAL_VIDEO_FILES:
                    # MOV\t
                    s += "%s\t" % current_mov
                if SYLLABLE:
                    # Ans\tOther
                    s += "%s\t%s" % (SyllableVars['Syllable'], SyllableVars['Other'])
                self.outFile.write(s)
                self.outFile.write('\n')
                # make sure this get written to disk now, in case we crash before buffers are flushed
                self.outFile.flush()
                os.fsync(self.outFile.fileno())

            if self.abort:
                break
            if bDone:
                break

            # line 2952 in ContPanView.cpp - GOBACK logic here
            if 'Bounce back if miss' in var and (var['trials in a row'] or var['signals in a row']):
                # print 'checking if bounce-back'
                bGoBack = 0
                if var['trials in a row']:
                    # look at trials
                    if nTrial+1 >= var['Bounce back if miss']:
                        # see if we should go back
                        bGoBack = 1
                        for i in range(nTrial, nTrial-var['Bounce back if miss'], -1):
                            if self.bCorrect[i]:
                                # we are done - normal: do not go back
                                bGoBack = 0
                                break
                if not bGoBack and var['signals in a row']:
                    # look at signal trials
                    if self.nSignal_current >= var['Bounce back if miss']:
                        # see if we should go back
                        bGoBack = 1
                        nToCheck = var['Bounce back if miss']
                        for i in range(nTrial, -1, -1):
                            if self.nTrialType[i] != pb.SIGNAL:
                                continue
                            if self.bCorrect[i]:
                                # we are done - normal: do not go back
                                bGoBack = 0
                                break
                            nToCheck -= 1
                            if nToCheck <= 0:
                                # we are done
                                break
                if bGoBack:
                    print('BOUNCE BACK')
                    self.abort = 5 # to indicate this type of bounceback
                    break

            if bAdjSignalDifficulty and self.nTrialType[nTrial] != pb.NO_SIG and \
               self.nTrialType[nTrial] != pb.SIGNAL_FORCED and not self.bIsProbe:
                # this was a signal trial. Determine next signal level if bAdjSignalDifficulty
                nDir=self.nDir
                if not nDir:
                    nDir = -1   # initial step direction
                if self.bUpDownIsIntensity:
                    self.fSignalInten += nDir * self.fSignalStepSize
                    if self.fSignalInten >= var['Stop if intensity goes up to']:
                        if not ROBOT_SIM and not SIMULATION:
                            WarnDlg(self, "Next level is >= Max STOP intensity. Adaptive out of bounds.")
                        self.outFile.write("Adaptive out of bounds. (>= Max STOP intensity)\n")
                        break
                    if self.fSignalInten <= var['Stop if intensity goes down to']:
                        if not ROBOT_SIM and not SIMULATION:
                            WarnDlg(self, "Next level is <= Min STOP intensity. Adaptive out of bounds.")
                        self.outFile.write("Adaptive out of bounds. (<= Min STOP intensity)\n")
                        break
                else:
                    # NEW WAV up/down method
                    prev_row = self.iSignalWavRow
                    step = int(nDir * -1 * self.fSignalStepSize)
                    self.iSignalWavRow = prev_row + step
                    if self.iSignalWavRow >= n_wav_rows_this_phase:
                        if var['Stay in if out of bounds']:
                            msg = 'Next WAV row (%d) is > number of rows (%d). Will stay at row %d.' % (
                                    self.iSignalWavRow+1, n_wav_rows_this_phase, n_wav_rows_this_phase)
                            if not ROBOT_SIM and not SIMULATION:
                                WarnDlg(self, msg)
                            self.outFile.write(msg+'\n')
                            self.iSignalWavRow = n_wav_rows_this_phase - 1
                        else:
                            msg = 'Next WAV row (%d) is > number of rows (%d). Step is %d. Adaptive out of bounds.' % (
                                    self.iSignalWavRow+1, n_wav_rows_this_phase, step)
                            if not ROBOT_SIM and not SIMULATION:
                                WarnDlg(self, msg)
                            self.outFile.write(msg+'\n')
                            break
                    if self.iSignalWavRow < 0:
                        if var['Stay in if out of bounds']:
                            msg = 'Next WAV row (%d) is < 1. Will stay at row 1.' % (
                                    self.iSignalWavRow+1, )
                            if not ROBOT_SIM and not SIMULATION:
                                WarnDlg(self, msg)
                            self.outFile.write(msg+'\n')
                            self.iSignalWavRow = 0
                        else:
                            msg = 'Next WAV row (%d) is < 1. Step is %d. Adaptive out of bounds.' % (
                                    self.iSignalWavRow+1, step)
                            if not ROBOT_SIM and not SIMULATION:
                                WarnDlg(self, msg)
                            self.outFile.write(msg+'\n')
                            break

        self.nTrialsSoFar[nPhase] += nTrial+1

        if not self.simulation_output_only:
            # write to file
            nReversalsUsed=0
            if mode == 'UPDOWN':
                nReversalsUsed = self.nReversals - var['Ignore']
                if nReversalsUsed > 0:
                    nSkip = 0
                    # the 2 lines below were disabled on 3/6/15
                    #if nReversalsUsed > MAX_NUM_REVERSALS_TO_USE:
                    #    nSkip = nReversalsUsed - MAX_NUM_REVERSALS_TO_USE
                    fThrMean, strThrSD, fThrMin, fThrMax = MeanSdMinMax(nReversalsUsed-nSkip,
                                                                             self.fThrEst[nSkip:])

                    s = "Threshold: Mean = %.1f dB StdDev = %s dB Range = %.1f dB\n" % (fThrMean,
                                                                                        strThrSD,
                                                                                        fThrMax-fThrMin )
                    self.outFile.write(s)
                    s = "Number of reversals used: %d\n" % (nReversalsUsed-nSkip)
                    self.outFile.write(s)

            self.outFile.write("Signal Hit Rate:\t%s\n" % strSignalHitRate)
            if ABX:
                for i in range(n_abx_wav_rows_this_phase):
                    if self.nSignalByRow[nPhase][i]:
                        strSignalHitRateThisRow = '%.0f' % (100.0*float(self.nCorrectSignalByRow[nPhase][i])/self.nSignalByRow[nPhase][i])
                    else:
                        strSignalHitRateThisRow = 'N/A'
                    if self.nCorrectSignalByRow[nPhase][i]:
                        strLatencyThisRow = '%.0f ms' % (1e3*self.latencyByRow[nPhase][i]/self.nCorrectSignalByRow[nPhase][i])
                    else:
                        strLatencyThisRow = 'N/A'
                    self.outFile.write("Signal Hit Rate for row %d:\t%s %% (N=%d)\tAvg latency is %s (N=%d)\n" % (
                        i+1,
                        strSignalHitRateThisRow,
                        self.nSignalByRow[nPhase][i],
                        strLatencyThisRow,
                        self.nCorrectSignalByRow[nPhase][i]
                        ))
            self.outFile.write("Probe  Hit Rate:\t%s\n" % strProbeHitRate)
            self.outFile.write("False Alarm Rate:\t%s\n" % strFalseAlarmRate)
            self.outFile.write("Number of no-sig trials:\t%s\nNumber of probe trials:\t%s\nNumber of signal trials:\t%s\n" % \
                               (self.nCatch[nPhase], self.nProbe[nPhase], self.nSignal[nPhase]))
            self.outFile.write("\n")
            # make sure this get written to disk now, in case we crash before buffers are flushed
            self.outFile.flush()
            os.fsync(self.outFile.fileno())

        if not SIMULATION:
            # ------- begin NOT SIMULATION block --------
            if not ROBOT_SIM:
                if mode == 'UPDOWN' and nReversalsUsed > 0:
                    s = "nCatch=%d, nProbe=%d, nSignal=%d, ThrMean=%.1f, ThrSD=%s, ThrMin=%.1f, ThrMax=%.1f" % (
                        self.nCatch[nPhase], self.nProbe[nPhase], self.nSignal[nPhase], fThrMean, strThrSD, fThrMin, fThrMax)
                else:
                    s = "nCatch=%d, nProbe=%d, nSignal=%d" % (self.nCatch[nPhase], self.nProbe[nPhase], self.nSignal[nPhase])
                WarnDlg(self, s)

##            print '--- begin times ---'
##            for i in range(analog_io.GetDbgTimeIndex()):
##                print i, analog_io.GetDbgTime(i)
##            print '--- end times ---'

            stat = self.StopSoundThread()
            if stat:
                self.outFile.write('Error stopping soundthread.\n')
                self.abort = 1
            if REPEATING_VIDEO:
                self.StopVideoThread()
            # ------- end NOT SIMULATION block --------

    def StartSoundThread(self, var, catchAndProbeVars, signal_intensity, signal_wav_files, backgnd_wav_files,
                         backgnd2_wav_files, probe_wav_files, nosignal_wav_files, stim_dur, ch2_dur):
        self.sample_rate = 0 # allows each phase to have diff sample rate
        stat = analog_io.Init(self.hwnd, 0)
        if stat:
            ErrorDlg(self, 'ERROR from analog_io.Init: %s' % stat)
            return stat

        # load Signals first, as we always use those. Will set up buffers.
        stat = self.LoadWaves(out_chan=0, wav_files=signal_wav_files,
                              level=signal_intensity, typ=0,
                              stim_dur=stim_dur, ch2_dur=ch2_dur)
        if stat:
            return stat

        if self.n_backgnds:
            stat = self.LoadWaves(out_chan=0, wav_files=backgnd_wav_files,
                                  level=var['Background intensity'], typ=1,
                                  stim_dur=stim_dur, ch2_dur=ch2_dur)
            if stat:
                return stat

        if self.n_backgnd2s:
            stat = self.LoadWaves(out_chan=1, wav_files=backgnd2_wav_files,
                                  level=var['Background2 intensity'], typ=2,
                                  stim_dur=stim_dur, ch2_dur=ch2_dur)
            if stat:
                return stat

        if self.n_probes:
            stat = self.LoadWaves(out_chan=0, wav_files=probe_wav_files,
                                  level=catchAndProbeVars['Probe intensity'], typ=3,
                                  stim_dur=stim_dur, ch2_dur=ch2_dur)
            if stat:
                return stat

        if self.n_nosignals:
            stat = self.LoadWaves(out_chan=0, wav_files=nosignal_wav_files,
                                  level=var['No-signal intensity'], typ=4,
                                  stim_dur=stim_dur, ch2_dur=ch2_dur)
            if stat:
                return stat

        self.sound_thread = ThreadClass()
        self.sound_thread.start() # invokes run() in a separate thread of control
        print('self.sound_thread.isAlive()=',self.sound_thread.isAlive())
        time.sleep(0.1) # make sure we don't call Signal before backgnd has started

        self.analog_io = analog_io

    def StopSoundThread(self):
        self.analog_io = None

        # make this a method of sound_thread or analog_io instead?
        # stat = self.StopSoundThread()
        stat = analog_io.Stop()
        if stat:
            ErrorDlg(self, 'Error from analog_io.Stop: %s' % stat)
            return 1

        # wait for stim to finish
        t0 = time.clock()
        while self.sound_thread.isAlive():
            time.sleep(0.1) # FREE UP CPU ? MAKE EVENT DRIVEN?
            wx.Yield()
        print('It took %.3f sec to stop sound thread' % (time.clock() - t0))
        return 0

    def StartVideoThread(self):
        movie_to_sound_delay_ms = int(1e3*self.varStimFirst['nostep']['Video sound delay'])
        video_offset_jitter_ms = abs(int(1e3*self.varStimFirst['nostep']['Video offset jitter']))
        output_period_ms = int(0.5 + 1e3 * self.buff_size / float(self.sample_rate))
        self.video_thread = VideoThreadClass(movie_to_sound_delay_ms, video_offset_jitter_ms,
                                             output_period_ms,
                                             self.video_offsets, self.video_lengths,
                                             self.backgnd_mov_index_to_long_video_index)
        self.video_thread.start() # invokes run() in a separate thread of control
        print('self.video_thread.isAlive()=',self.video_thread.isAlive())
        time.sleep(3.0) # make sure we don't call Signal before backgnd has started

    def StopVideoThread(self):
        # StopSoundThread() will cause video thread to die

        # wait for it to die
        t0 = time.clock()
        while self.video_thread.isAlive():
            time.sleep(0.1) # FREE UP CPU ? MAKE EVENT DRIVEN?
            wx.Yield()
        print('It took %.3f sec for video thread to stop' % (time.clock() - t0))
        return 0

    def gen_flat_top(self, nsamps):
        # flat top window

        # a1
        pi2 = 2.0 * numpy.pi
        d = pi2 / (nsamps-1)
        self.flat_top = -1.93 * numpy.cos(numpy.arange(0.0, pi2+d, d, dtype=numpy.float32))[:nsamps]

        # a2
        pi4 = 4.0 * numpy.pi
        d = pi4 / (nsamps-1)
        self.flat_top += 1.29 * numpy.cos(numpy.arange(0.0, pi4+d, d, dtype=numpy.float32))[:nsamps]
        
        # a3
        pi6 = 6.0 * numpy.pi
        d = pi6 / (nsamps-1)
        self.flat_top += -0.388 * numpy.cos(numpy.arange(0.0, pi6+d, d, dtype=numpy.float32))[:nsamps]

        # a4
        pi8 = 8.0 * numpy.pi
        d = pi8 / (nsamps-1)
        self.flat_top += 0.032 * numpy.cos(numpy.arange(0.0, pi8+d, d, dtype=numpy.float32))[:nsamps]

        self.flat_top += 1.0

    def input_cal_using_oscope(self, event):
        CalInputUsingScopeVars['V_FS'] = self.voltage_input_full_scale
        dlg = ModelessDlg(self, 'Input cal using o\'scope', '', ['Start', 'Quit'],
                          CalInputUsingScopeParams, CalInputUsingScopeVars)
        dlg.CenterOnScreen()
        dlg.Show()
        while not dlg.sButt == 'Quit':
            wx.Yield()
            if dlg.sButt == 'Start':
                dlg.GetData() # will write into dlg.filenameVars, which, because pass by ref, is our copy
                break
        if dlg.sButt == 'Quit':
            dlg.Destroy()
            return

        dlg.Destroy()
        wx.BeginBusyCursor()

        # user parameters

        attn = CalInputUsingScopeVars['Attenuation']
        freq = CalInputUsingScopeVars['Frequency']
        play_record_time = CalInputUsingScopeVars['Run time']
        self.voltage_input_full_scale = CalInputUsingScopeVars['V_FS'] # corresponds to 32768 counts (16-bit)

        counts_to_volts = self.voltage_input_full_scale/math.pow(2.0,31.0) # 32-bit (24-bit A/D shifted left)
        self.sample_rate = 22050
        print('self.sample_rate=',self.sample_rate)
        max_f = freq * 10 # you can zoom in, so this can be on the big side
        
        deltaF = float(self.sample_rate) / NSAMPS # python float is C double
        freq_bin = int(0.5+freq/deltaF)
        freq = deltaF * freq_bin # make freq a multiple of deltaF, so that windowing not needed
                
        if attn < 0:
            WarnDlg(self, "You cannot go that high")
            attn = 0

        # init
        stat = analog_io.Init(self.hwnd, 1)
        if stat:
            if stat == ERR_DSC_CREATE:
                ErrorDlg(self, 'ERROR setting up recording')
            else:
                ErrorDlg(self, 'ERROR from analog_io.Init: %s' % stat)
            return 1

        nsamps_per_output_buff = NSAMPS # see if we can avoid need to window
        nsamps_per_input_pri_buff = NSAMPS # same as output buff, because we want total play time = total record time

        n_input_blocks = int(math.ceil(float(play_record_time)*self.sample_rate/nsamps_per_input_pri_buff))
        if n_input_blocks < 1:
            n_input_blocks = 1
        n_skip_blocks = int(math.ceil(float(SECONDS_TO_SKIP)*self.sample_rate/nsamps_per_input_pri_buff))
        n_input_blocks += n_skip_blocks
        nsamps_per_input_sec_buff = nsamps_per_input_pri_buff * n_input_blocks
        play_record_time = nsamps_per_input_sec_buff/float(self.sample_rate)

        slope = 2.0 * numpy.pi * freq / self.sample_rate
        c_input_buff = (ctypes.c_float * nsamps_per_input_sec_buff)()
        print('nsamps_per_input_sec_buff=',nsamps_per_input_sec_buff)
        
        stat = analog_io.AllocateBuffers(self.sample_rate, nsamps_per_output_buff, 0, 0,
                                         1, 0, 0, 0,
                                         nsamps_per_input_pri_buff, nsamps_per_input_sec_buff, # record
                                         ctypes.pointer(c_input_buff), # record
                                         )
        if stat:
            ErrorDlg(self, 'Error from analog_io.AllocateBuffers: %s' % stat)
            return 1

#       intensity now set when wav files are loaded into memory
        c_output_buff = (ctypes.c_short * nsamps_per_output_buff)() # buffer to hold waveform
        # wave = 32767 * numpy.sin(numpy.arange(0.0, slope*(n+1), slope, dtype=numpy.float32)) # i think i remember this as using a float32 number to add
        wave = 32767 * numpy.sin(numpy.arange(0.0, slope*(nsamps_per_output_buff), slope, dtype=numpy.float64)) # better precision needed (?)
        sound_data = numpy.array(wave, dtype=numpy.int16)
        c_output_buff[:] = sound_data[:nsamps_per_output_buff]

        typ = 1 # background (chan 1)
        atten_times100 = int(attn * 100)
        stat = analog_io.LoadWav(typ, 0, nsamps_per_output_buff, ctypes.pointer(c_output_buff), atten_times100)
        if stat:
            ErrorDlg(self, 'Error from analog_io.LoadWav: %s' % stat)
            return 1

        self.sound_thread = ThreadClass()
        self.sound_thread.start() # invokes run() in a separate thread of control

        # now wait
        time.sleep(1.1*play_record_time)

        stat = self.StopSoundThread()

        with warnings.catch_warnings():
            warnings.filterwarnings('ignore',
                                    '^Item size computed from the PEP 3118 buffer format string does not match the actual item size\.$',)
            mic_data = numpy.array(c_input_buff, dtype=numpy.float32)
            

        mx=mic_data.max()
        mn=mic_data.min()
        fs = mx
        if -mn > fs:
            fs = -mn
        fs = float(fs) / math.pow(2.0,31.0) # fs = percent of FS
        print('mic_data min,max as 16-bit: %.0f, %.0f as Volts: %.2f, %.2f, %.2f%%FS' % \
              (mn/65536.0, mx/65536.0, mn*counts_to_volts, mx*counts_to_volts, 100.0*fs))

        mic_data *= counts_to_volts

        if CalInputUsingScopeVars['Show time history']:
            # plot full time history ----------------------
            delta_t = 1.0/self.sample_rate
            n_to_plot = len(mic_data)
            t = numpy.arange(0, delta_t+n_to_plot*delta_t, delta_t)
            subplot = plt.subplot(1,1,1) # only needed if I want to call subplot methods
            plt.plot(t[:n_to_plot],mic_data[:n_to_plot],'r')
            plt.title('Time history') # optional
            plt.xlabel('seconds')        # optional
            plt.ylabel('Volts')       # optional
            subplot.set_autoscale_on(True)
            plt.grid(True)
            plt.show()

        # FFT plot setup ----------------------
        f = numpy.arange(deltaF, deltaF*NSAMPS, deltaF)[:NSAMPS/2]
        f *= 1e-3

        if max_f:
            n_to_plot = int(max_f / deltaF)
            if n_to_plot > NSAMPS/2:
                n_to_plot = NSAMPS/2
        else:
            n_to_plot = NSAMPS/2

        # Do FFT---------------------
        offset = int(SECONDS_TO_SKIP * self.sample_rate)

        num_aves = 0
        ave_result = numpy.zeros(dtype=numpy.float32, shape=(NSAMPS/2,))
        while offset+NSAMPS <= nsamps_per_input_sec_buff:
            time_hist = mic_data[offset:offset+NSAMPS].copy()
            dc = time_hist.mean() # remove DC offset because we window
            time_hist -= dc
            time_hist *= self.flat_top

            mag_sqrd = self.fft_mag_sqrd(NSAMPS, time_hist)
            dBV = self.sqrd_to_dB(1.0, NSAMPS, mag_sqrd)
            print('### %.3f kHz is at %.2f dBV' % \
                  (1e-3*freq_bin*deltaF, dBV[freq_bin] ))
            ave_result += mag_sqrd

            offset += NSAMPS/2 # 50% overlap
            # offset += NSAMPS # 0% overlap
            num_aves += 1

        ave_result /= num_aves
        dBV = self.sqrd_to_dB(1.0, NSAMPS, ave_result)

        calc_meas_Vpp = 2*1.414 * (10.0 ** (dBV[freq_bin]/20.0))
        print('%.3f kHz average is %.2f dBV (%.3fVpp)' % \
              (1e-3*freq_bin*deltaF, dBV[freq_bin], calc_meas_Vpp))

        anno1 = 'V_FS setting = %.3f, giving tone of %.3fVpp.' % (self.voltage_input_full_scale, calc_meas_Vpp)
        anno2 = 'If your measurment disagrees, correct V_FS by'
        anno3 = '   multiplying it by (measuredVpp / %.3f)' % calc_meas_Vpp
        anno = anno1+'\n'+anno2+'\n'+anno3

        subplot = plt.subplot(1,1,1) # only needed if I want to call subplot methods
        plt.plot(f[:n_to_plot],dBV[:n_to_plot],'r')
        plt.title('Calibrate using oscilloscpe') # optional
        plt.text(3.1, -25.0, anno, bbox={'facecolor':'red', 'alpha':1.0, 'pad':10})
        plt.xlabel('kHz')        # optional
        plt.ylabel('dBV')       # optional
        subplot.set_autoscale_on(True)
        #subplot.set_ylim(150.0, 300.0)
        plt.grid(True)
        plt.show()

        dlg.Destroy()

        self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing
        wx.EndBusyCursor()

    def input_cal_using_acoustic_calibrator(self, event):
        CalInputUsingAcousCalVars['V_FS'] = self.voltage_input_full_scale
        dlg = ModelessDlg(self, 'Input cal using calibrator', '', ['Start', 'Quit'],
                          CalInputUsingAcousCalParams, CalInputUsingAcousCalVars)
        dlg.CenterOnScreen()
        dlg.Show()
        while not dlg.sButt == 'Quit':
            wx.Yield()
            if dlg.sButt == 'Start':
                dlg.GetData() # will write into dlg.filenameVars, which, because pass by ref, is our copy
                break
        if dlg.sButt == 'Quit':
            dlg.Destroy()
            return

        dlg.Destroy()
        wx.BeginBusyCursor()

        # user parameters
        sens = CalInputUsingAcousCalVars['Mic sens'] # in mV/Pa
        sens *= 1000.0  # to microvolts
        sens /= 50118.7 # "subtract" 94 dB - now it's uV at 0 dB SPL
        print('\nsens is %.5f uV RMS at 0 dB SPL' % sens)
        sens *= 1e-6  # in V

        micGain_dB = CalInputUsingAcousCalVars['Mic gain']
        mic_gain = 10.0 ** (micGain_dB/20.0)
#
        play_record_time = CalInputUsingAcousCalVars['Run time']
        self.voltage_input_full_scale = CalInputUsingAcousCalVars['V_FS'] # corresponds to 32768 counts (16-bit)

        counts_to_volts = self.voltage_input_full_scale/math.pow(2.0,31.0) # 32-bit (24-bit A/D shifted left)
        self.sample_rate = 22050
        print('self.sample_rate=',self.sample_rate)
        max_f = 10e3 # you can zoom in, so this can be on the big side
        
        deltaF = float(self.sample_rate) / NSAMPS # python float is C double

        # init
        stat = analog_io.Init(self.hwnd, 1)
        if stat:
            if stat == ERR_DSC_CREATE:
                ErrorDlg(self, 'ERROR setting up recording')
            else:
                ErrorDlg(self, 'ERROR from analog_io.Init: %s' % stat)
            return 1

        nsamps_per_output_buff = NSAMPS # see if we can avoid need to window
        nsamps_per_input_pri_buff = NSAMPS # same as output buff, because we want total play time = total record time

        n_input_blocks = int(math.ceil(float(play_record_time)*self.sample_rate/nsamps_per_input_pri_buff))
        if n_input_blocks < 1:
            n_input_blocks = 1
        n_skip_blocks = int(math.ceil(float(SECONDS_TO_SKIP)*self.sample_rate/nsamps_per_input_pri_buff))
        n_input_blocks += n_skip_blocks
        nsamps_per_input_sec_buff = nsamps_per_input_pri_buff * n_input_blocks
        play_record_time = nsamps_per_input_sec_buff/float(self.sample_rate)

        c_input_buff = (ctypes.c_float * nsamps_per_input_sec_buff)()
        print('nsamps_per_input_sec_buff=',nsamps_per_input_sec_buff)
        
        stat = analog_io.AllocateBuffers(self.sample_rate, nsamps_per_output_buff, 0, 0,
                                         1, 0, 0, 0,
                                         nsamps_per_input_pri_buff, nsamps_per_input_sec_buff, # record
                                         ctypes.pointer(c_input_buff), # record
                                         )
        if stat:
            ErrorDlg(self, 'Error from analog_io.AllocateBuffers: %s' % stat)
            return 1

#       silent output
        c_output_buff = (ctypes.c_short * nsamps_per_output_buff)() # buffer to hold waveform
        sound_data = numpy.zeros(dtype=numpy.int16, shape=(nsamps_per_output_buff,))
        c_output_buff[:] = sound_data[:nsamps_per_output_buff]

        typ = 1 # background (chan 1)
        atten_times100 = 0
        stat = analog_io.LoadWav(typ, 0, nsamps_per_output_buff, ctypes.pointer(c_output_buff), atten_times100)
        if stat:
            ErrorDlg(self, 'Error from analog_io.LoadWav: %s' % stat)
            return 1

        self.sound_thread = ThreadClass()
        self.sound_thread.start() # invokes run() in a separate thread of control

        # now wait
        time.sleep(1.1*play_record_time)

        stat = self.StopSoundThread()

        with warnings.catch_warnings():
            warnings.filterwarnings('ignore',
                                    '^Item size computed from the PEP 3118 buffer format string does not match the actual item size\.$',)
            mic_data = numpy.array(c_input_buff, dtype=numpy.float32)
        mx=mic_data.max()
        mn=mic_data.min()
        fs = mx
        if -mn > fs:
            fs = -mn
        fs = float(fs) / math.pow(2.0,31.0) # fs = percent of FS
        print('mic_data min,max as 16-bit: %.0f, %.0f as Volts: %.2f, %.2f, %.2f%%FS' % \
              (mn/65536.0, mx/65536.0, mn*counts_to_volts, mx*counts_to_volts, 100.0*fs))

        mic_data *= counts_to_volts

        if CalInputUsingAcousCalVars['Show time history']:
            # plot full time history ----------------------
            delta_t = 1.0/self.sample_rate
            n_to_plot = len(mic_data)
            t = numpy.arange(0, delta_t+n_to_plot*delta_t, delta_t)
            subplot = plt.subplot(1,1,1) # only needed if I want to call subplot methods
            plt.plot(t[:n_to_plot],mic_data[:n_to_plot],'r')
            plt.title('Time history') # optional
            plt.xlabel('seconds')        # optional
            plt.ylabel('Line-in Volts')       # optional
            subplot.set_autoscale_on(True)
            plt.grid(True)
            plt.show()

        # FFT plot setup ----------------------
        mic_data *= 1.0 / mic_gain

        f = numpy.arange(deltaF, deltaF*NSAMPS, deltaF)[:NSAMPS/2]
        f *= 1e-3

        if max_f:
            n_to_plot = int(max_f / deltaF)
            if n_to_plot > NSAMPS/2:
                n_to_plot = NSAMPS/2
        else:
            n_to_plot = NSAMPS/2

        # Do FFT---------------------
        offset = int(SECONDS_TO_SKIP * self.sample_rate)

        num_aves = 0
        ave_result = numpy.zeros(dtype=numpy.float32, shape=(NSAMPS/2,))
        while offset+NSAMPS <= nsamps_per_input_sec_buff:
            time_hist = mic_data[offset:offset+NSAMPS].copy()
            dc = time_hist.mean() # remove DC offset because we window
            time_hist -= dc
            time_hist *= self.flat_top

            mag_sqrd = self.fft_mag_sqrd(NSAMPS, time_hist)
            dB_SPL = self.sqrd_to_dB(sens, NSAMPS, mag_sqrd)
            indx_max = dB_SPL.argmax()
            print('### Peak is %.2f dB SPL at %.3f kHz' % \
                  (dB_SPL[indx_max], 1e-3*indx_max*deltaF))
            ave_result += mag_sqrd

            offset += NSAMPS/2 # 50% overlap
            # offset += NSAMPS # 0% overlap
            num_aves += 1

        ave_result /= num_aves
        dB_SPL = self.sqrd_to_dB(sens, NSAMPS, ave_result)

        indx_max = dB_SPL.argmax()
        print('Peak is %.2f dB SPL at %.3f kHz' % \
              (dB_SPL[indx_max], 1e-3*indx_max*deltaF))

        anno1 = 'V_FS setting = %.3f, giving tone of %.2f dB SPL at %.2f KHz.' % \
                (self.voltage_input_full_scale, dB_SPL[indx_max], 1e-3*indx_max*deltaF)
        anno2 = 'V_FS should be %.3f to get %.1f dB SPL' % \
                (self.voltage_input_full_scale * 10.0 ** ((CalInputUsingAcousCalVars['Acoustic output']-dB_SPL[indx_max])/20.0),
                 CalInputUsingAcousCalVars['Acoustic output'])
        anno = anno1+'\n'+anno2#+'\n'+anno3

        input_info = 'Input is %.0f%% FS.' % (100.0*fs)
        if fs > .99:
            input_color = 'red'
        elif fs < 0.1:
            input_color = 'yellow'
        else:
            input_color = 'green'

        subplot = plt.subplot(1,1,1) # only needed if I want to call subplot methods
        plt.plot(f[:n_to_plot],dB_SPL[:n_to_plot],'r')
        plt.title('Calibrate using acoustic calibrator') # optional
        plt.text(1.8, 80.0, anno, bbox={'facecolor':'red', 'alpha':1.0, 'pad':10})
        plt.text(5.0, 40.0, input_info, bbox={'facecolor':input_color, 'alpha':1.0, 'pad':10})
        plt.xlabel('kHz')        # optional
        plt.ylabel('dB SPL')       # optional
        subplot.set_autoscale_on(True)
        #subplot.set_ylim(150.0, 300.0)
        plt.grid(True)
        plt.show()

        dlg.Destroy()

        self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing
        wx.EndBusyCursor()

    def meas_iec(self, event):
        if not self.bRunning:
            self.controlPanel.meas_iec_button.Disable()
        IECVars['V_FS'] = self.voltage_input_full_scale
        dlg = ModelessDlg(self, 'IEC', '', ['Start', 'Quit'],
                          IECParams, IECVars)
        dlg.CenterOnScreen()
        dlg.Show()
        while not dlg.sButt == 'Quit':
            wx.Yield()
            if dlg.sButt == 'Start':
                dlg.GetData() # will write into dlg.filenameVars, which, because pass by ref, is our copy

                # === BEGIN RUN IEC ==============
                wx.BeginBusyCursor()

                # user parameters
                sens = IECVars['Mic sens']
                sens *= 1000.0  # to microvolts
                sens /= 50118.7 # "subtract" 94 dB - now it's uV at 0 dB SPL
                print('\nsens is %.5f uV RMS at 0 dB SPL' % sens)
                sens *= 1e-6  # in V

                micGain_dB = IECVars['Mic gain']
                mic_gain = 10.0 ** (micGain_dB/20.0)

                attn = IECVars['Cal max intensity'] - IECVars['Intensity']
                freq = IECVars['Frequency']
                play_record_time = IECVars['Run time']
                self.voltage_input_full_scale = IECVars['V_FS'] # corresponds to 32768 counts (16-bit)

                counts_to_volts = self.voltage_input_full_scale/math.pow(2.0,31.0) # 32-bit (24-bit A/D shifted left)
                self.sample_rate = 22050
                print('self.sample_rate=',self.sample_rate)
                max_f = freq * 10 # you can zoom in, so this can be on the big side
                
                deltaF = float(self.sample_rate) / NSAMPS # python float is C double
                freq_bin = int(0.5+freq/deltaF)
                freq = deltaF * freq_bin # make freq a multiple of deltaF, so that windowing not needed
                        
                if attn < 0:
                    WarnDlg(self, "You cannot go that high")
                    attn = 0

                # init
                stat = analog_io.Init(self.hwnd, 1)
                if stat:
                    if stat == ERR_DSC_CREATE:
                        ErrorDlg(self, 'ERROR setting up recording')
                    else:
                        ErrorDlg(self, 'ERROR from analog_io.Init: %s' % stat)
                    return 1

                nsamps_per_output_buff = NSAMPS # see if we can avoid need to window
                nsamps_per_input_pri_buff = NSAMPS # same as output buff, because we want total play time = total record time

                n_input_blocks = int(math.ceil(float(play_record_time)*self.sample_rate/nsamps_per_input_pri_buff))
                if n_input_blocks < 1:
                    n_input_blocks = 1
                n_skip_blocks = int(math.ceil(float(SECONDS_TO_SKIP)*self.sample_rate/nsamps_per_input_pri_buff))
                n_input_blocks += n_skip_blocks
                nsamps_per_input_sec_buff = nsamps_per_input_pri_buff * n_input_blocks
                play_record_time = nsamps_per_input_sec_buff/float(self.sample_rate)

                slope = 2.0 * numpy.pi * freq / self.sample_rate
                c_input_buff = (ctypes.c_float * nsamps_per_input_sec_buff)()
                
                stat = analog_io.AllocateBuffers(self.sample_rate, nsamps_per_output_buff, 0, 0,
                                                 1, 0, 0, 0,
                                                 nsamps_per_input_pri_buff, nsamps_per_input_sec_buff, # record
                                                 ctypes.pointer(c_input_buff), # record
                                                 )
                if stat:
                    ErrorDlg(self, 'Error from analog_io.AllocateBuffers: %s' % stat)
                    return 1

        #       intensity now set when wav files are loaded into memory
                c_output_buff = (ctypes.c_short * nsamps_per_output_buff)() # buffer to hold waveform
                # wave = 32767 * numpy.sin(numpy.arange(0.0, slope*(n+1), slope, dtype=numpy.float32)) # i think i remember this as using a float32 number to add
                wave = 32767 * numpy.sin(numpy.arange(0.0, slope*(nsamps_per_output_buff), slope, dtype=numpy.float64)) # better precision needed (?)
                sound_data = numpy.array(wave, dtype=numpy.int16)
                c_output_buff[:] = sound_data[:nsamps_per_output_buff]

                typ = 1 # background (chan 1)
                atten_times100 = int(attn * 100)
                stat = analog_io.LoadWav(typ, 0, nsamps_per_output_buff, ctypes.pointer(c_output_buff), atten_times100)
                if stat:
                    ErrorDlg(self, 'Error from analog_io.LoadWav: %s' % stat)
                    return 1

                self.sound_thread = ThreadClass()
                self.sound_thread.start() # invokes run() in a separate thread of control

                # now wait
                time.sleep(1.1*play_record_time)

                stat = self.StopSoundThread()

                with warnings.catch_warnings():
                    warnings.filterwarnings('ignore',
                                            '^Item size computed from the PEP 3118 buffer format string does not match the actual item size\.$',)
                    mic_data = numpy.array(c_input_buff, dtype=numpy.float32)
                mx=mic_data.max()
                mn=mic_data.min()
                fs = mx
                if -mn > fs:
                    fs = -mn
                fs = float(fs) / math.pow(2.0,31.0) # fs = percent of FS
                print('mic_data min,max as 16-bit: %.0f, %.0f as Volts: %.2f, %.2f, %.2f%%FS' % \
                      (mn/65536.0, mx/65536.0, mn*counts_to_volts, mx*counts_to_volts, 100.0*fs))

                mic_data *= counts_to_volts

                if IECVars['Show time history']:
                    # time history plot ----------------------
                    delta_t = 1.0/self.sample_rate
                    n_to_plot = len(mic_data)
                    t = numpy.arange(0, delta_t+n_to_plot*delta_t, delta_t)
                    subplot = plt.subplot(1,1,1) # only needed if I want to call subplot methods
                    plt.plot(t[:n_to_plot],mic_data[:n_to_plot],'r')
                    plt.title('mic_data') # optional
                    plt.xlabel('time')        # optional
                    plt.ylabel('V')       # optional
                    subplot.set_autoscale_on(True)
                    plt.grid(True)
                    plt.show()

                # FFT plot setup ----------------------

                mic_data *= 1.0 / mic_gain

                f = numpy.arange(deltaF, deltaF*NSAMPS, deltaF)[:NSAMPS/2]
                f *= 1e-3

                if max_f:
                    n_to_plot = int(max_f / deltaF)
                    if n_to_plot > NSAMPS/2:
                        n_to_plot = NSAMPS/2
                else:
                    n_to_plot = NSAMPS/2

                # Do FFT---------------------
                offset = int(SECONDS_TO_SKIP * self.sample_rate)

                num_aves = 0
                ave_result = numpy.zeros(dtype=numpy.float32, shape=(NSAMPS/2,))
                while offset+NSAMPS <= nsamps_per_input_sec_buff:
                    time_hist = mic_data[offset:offset+NSAMPS].copy()
                    dc = time_hist.mean() # remove DC offset because we window
                    time_hist -= dc
                    time_hist *= self.flat_top

                    mag_sqrd = self.fft_mag_sqrd(NSAMPS, time_hist)
                    dB_SPL = self.sqrd_to_dB(sens, NSAMPS, mag_sqrd)
                    print('### %.3f kHz is %.2f dB SPL' % (1e-3*freq_bin*deltaF, dB_SPL[freq_bin]))
                    ave_result += mag_sqrd

                    offset += NSAMPS/2 # 50% overlap
                    # offset += NSAMPS # 0% overlap
                    num_aves += 1

                ave_result /= num_aves
                dB_SPL = self.sqrd_to_dB(sens, NSAMPS, ave_result)

                anno1 = 'V_FS setting = %.3f, giving tone of %.2f dB SPL' % \
                        (self.voltage_input_full_scale, dB_SPL[freq_bin])
                anno = anno1

                subplot = plt.subplot(1,1,1) # only needed if I want to call subplot methods
                plt.plot(f[:n_to_plot],dB_SPL[:n_to_plot],'r')
                plt.title('IEC') # optional
                plt.text(2.5, 50.0, anno, bbox={'facecolor':'red', 'alpha':1.0, 'pad':10})
                plt.xlabel('kHz')        # optional
                plt.ylabel('dB SPL')       # optional
                subplot.set_autoscale_on(True)
                #subplot.set_ylim(150.0, 300.0)
                plt.grid(True)
                plt.show()

                self.controlPanel.start_button.SetFocus() # prevent this button from being default to avoid inadvertant pressing
                wx.EndBusyCursor()
                # === END RUN IEC ==============
                dlg.sButt = ''

        dlg.Destroy()
        if not self.bRunning:
            self.controlPanel.meas_iec_button.Enable()


    def fft_mag(self, sens, NSAMPS, mic_data):
        convFactor = 2.0 / 1.414213562373 # fft returns 1/2 peak -> convert to RMS
        convFactor /= float(NSAMPS)
        convFactor /= sens # sens is V at 0 dB SPL
        fa = numpy.fft.rfft(mic_data[:NSAMPS])[0:NSAMPS/2]
        result = numpy.sqrt(fa.real*fa.real + fa.imag*fa.imag) * convFactor
        try:
            result = numpy.log10(result) * 20.0
        except:
            print('exception! result max, min:',result.max(), result.min())
        # result[0] = 0.0 # zero the DC part
        return result

    def fft_mag_sqrd(self, NSAMPS, mic_data):
        fa = numpy.fft.rfft(mic_data[:NSAMPS])[0:NSAMPS/2]
        result = fa.real*fa.real + fa.imag*fa.imag
        # result[0] = 0.0 # zero the DC part
        return result

    def sqrd_to_dB(self, sens, NSAMPS, sqrd_mag):
        convFactor = 2.0 / 1.414213562373 # fft returns 1/2 peak -> convert to RMS
        convFactor /= float(NSAMPS)
        convFactor /= sens # sens is V at 0 dB SPL
        result = numpy.sqrt(sqrd_mag) * convFactor
        try:
            result = numpy.log10(result) * 20.0
        except:
            print('exception! result max, min:',result.max(), result.min())
        return result

    def OnAbout(self, event):
        import matplotlib
        py_vers = '%s.%s.%s' % (sys.version_info[0:3])
        import distutils.sysconfig
        pth = distutils.sysconfig.get_python_lib(plat_specific=1)
        pywin_vers = open(os.path.join(pth, "pywin32.version.txt")).read().strip()
        vers_info = '\n\nVersions:\nThis program: %s\nPython: %s\nwxPython: %s\nnumpy: %s\nmatplotlib: %s\npywin: %s\n' % (
            VERSION, py_vers, wx.VERSION_STRING, numpy.__version__, matplotlib.__version__, pywin_vers )
        if USING_PYGAME:
            vers_info += 'pygame: %s\n' % pygame.__version__
        dlg = wx.MessageDialog(self, "This is the Soundwav Baby version of PsychoBaby.\n"
                               "Written March 2012 by Brandon Warren (bwarren@u.washington.edu)"+vers_info,
                              "About Soundwav Baby", wx.OK | wx.ICON_INFORMATION)
        dlg.ShowModal()
        dlg.Destroy()

class MyApp(wx.App):
    def OnInit(self):
        global g_frame
        self.frame = MyFrame(None, -1, "Soundwav Baby")
        g_frame = self.frame
        self.frame.Show(True)
        self.SetTopWindow(self.frame)
        self.Bind(wx.EVT_KEY_DOWN, self.OnKey) # wx.EVT_CHAR doesn't work if ControlPanel is focus

        return True

    def OnKey(self, evt):
        #if not self.frame.bInRun:
            # not in middle of run, so nothing to do here
            #evt.Skip(skip=True)
            #return
        keycode = evt.GetKeyCode()
        if self.frame.bInFCDlg:
            # print 'IN FC DLG: got key:',keycode
            if keycode == 49 or keycode == 325:
                self.frame.FCDlg_key_pressed = '1'
            elif keycode == 50 or keycode == 326:
                self.frame.FCDlg_key_pressed = '2'
            elif THREE_ALT and (keycode == 51 or keycode == 327):
                self.frame.FCDlg_key_pressed = '3'
        if SYLLABLE:
            evt.Skip(skip=True)
            return
        if keycode == wx.WXK_ESCAPE:
            self.frame.Abort(0)
        elif keycode == wx.WXK_ADD or keycode == wx.WXK_NUMPAD_ADD or keycode == 92 or keycode == 43: # 43=laptop "=+" key
            self.frame.tPlusKey2 = analog_io.GetTime()
            self.frame.tPlusKey = time.clock()
        elif keycode == 81 or keycode == 87:            
            # 'q' or 'w' pressed
            self.frame.tForceToy = time.clock() + 0.3  # time key was pressed. Add 300ms to cover gap between events when key held down.
            if not self.frame.bToyOn:
                ntoy = -1
                if keycode == 81:
                    ntoy = 0 # 'q': turn on 1st toy
                else:
                    ntoy = 1 # 'w': turn on 2nd toy
                self.frame.TurnOnToy(1.0, ntoy=ntoy) # turn on for at least 1 sec
            else:
                self.frame.TurnOffToy()
#                self.frame.timeEndToy = self.frame.tForceToy
        else:
            # print 'MyApp: got key',keycode
            evt.Skip(skip=True) # True=allows Alt to work.    False or comment out=Alt->Exit no work

if __name__ == '__main__':
    SIMULATION = False
    MAX_N_PHASES = 15 # was pb.N_PHASES May be overridden by cmd line arg
    USE_TRIAL_VIDEO = False
    FORCED_CHOICE = False  # goes with ABX or AB for now
    ABX = False # 3 interval if True (2 interval if not?)
    ABC = False # variation of ABX, but they supply the 3rd wav
    AB = False
    ADULT_SUBJECT = False
    THREE_ALT = False
    EYE_TRACK = False
    MULTI_TRIAL_VIDEO_FILES = False # was KAYLAH in v113-kaylah-v4 (added to main in v115)
    REPEATING_VIDEO = False         # added july 2015 - video repeats in background thread
    SYLLABLE = False
    ROBOT_SIM = False # like SIMULATION, but act like person is running it - play videos, sound, random resp latency, etc (for now, only kaylah --use_trial_video --multi_trial_video_files --repeating_video)
    nargs = len(sys.argv)
    if nargs > 1:
        # 1st arg is cmd filename. If >1, am specifying arguments
        for iarg in range(1,nargs):
            if sys.argv[iarg] == '--simulation':
                SIMULATION = True
            elif sys.argv[iarg][:15] == '--max_n_phases=':
                try:
                    i = int(sys.argv[iarg][15:])
                    MAX_N_PHASES = i
                except:
                    pass
                # print 'MAX_N_PHASES=',MAX_N_PHASES
            elif sys.argv[iarg] == '--use_trial_video':
                USE_TRIAL_VIDEO = True
            elif sys.argv[iarg] == '--forced_choice':
                FORCED_CHOICE = True
            elif sys.argv[iarg] == '--abx':
                ABX = True
                ADULT_SUBJECT = True
            elif sys.argv[iarg] == '--abc':
                ABC = True
                ABX = True
                ADULT_SUBJECT = True
            elif sys.argv[iarg] == '--three_alt':
                THREE_ALT = True
            elif sys.argv[iarg] == '--ab':
                AB = True
            elif sys.argv[iarg] == '--syllable':
                SYLLABLE = True
                hw.TOY_CONTROLLER = ''
            elif sys.argv[iarg] == '--multi_trial_video_files':
                MULTI_TRIAL_VIDEO_FILES = True
            elif sys.argv[iarg] == '--repeating_video':
                REPEATING_VIDEO = True
            elif sys.argv[iarg] == '--eye_track':
                EYE_TRACK = True
            elif sys.argv[iarg] == '--robot_sim':
                ROBOT_SIM = True
                random.seed(0) # so each run is the same
                # hw.TOY_CONTROLLER = '' # so it runs the same with or without toy controller

    USING_PYGAME = AB or EYE_TRACK

    if USING_PYGAME:
        try:
            os.putenv('SDL_VIDEODRIVER', 'windib') # may be needed for pygame.movie
        except:
            print('Error setting SDL_VIDEODRIVER.')
            print('Please email %s for assistance.' % config_phys.CONTACT_EMAIL)
            input('Press enter to close this window.')
            sys.exit(1)
        try:
            print('MAIN: about to import pygame')
            import pygame
        except ImportError as target:
            print('Software configuration error: %s.' % str(target))
            print('Please email %s for assistance.' % config_phys.CONTACT_EMAIL)
            input('Press enter to close this window.')
            sys.exit(1)

    if config_phys.RUN_TESTS_NOW:
        import doctest
        doctest.testmod()
    else:
        # print 'SIMULATION =',SIMULATION,' USE_TRIAL_VIDEO =',USE_TRIAL_VIDEO,' FORCED_CHOICE =',FORCED_CHOICE,' ABX =',ABX,' EYE_TRACK =',EYE_TRACK,' AB =',AB,' ABC =',ABC,' THREE_ALT=',THREE_ALT
        app = MyApp(0)
        app.MainLoop()
