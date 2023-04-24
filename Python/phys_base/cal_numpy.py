version = '2013-08-01'  # version of this module

# numpy version of cal.py. Update both together.

import wx
import os
import math
import numpy
from numpy import rec

NSAMPS = 16384

# values for import file formats
SIMPLE = 1  # text lines of: freq-in-kHz max-dB-SPL
SANDRA = 2  # Sandra from Rudolf's lab

# values for export file formats
ONE_KHZ = 2 # output only multiples of 1 kHz

# indexes for header
NCHANS_INDX = 3
SRATE_INDX = 4
BLKSZ_INDX = 5
NFPTS_INDX = 6
CHANS_INDX = 7

xducer_cal_table = {}
time_domain_table = {}
wav_cal_table = {} # for calibrating wav files
for i in range(2):
    xducer_cal_table[i] = numpy.zeros(shape=(NSAMPS/2,), dtype=numpy.float32)
    time_domain_table[i] = numpy.zeros(shape=(NSAMPS,), dtype=numpy.float32)
    wav_cal_table[i] = {}

VERS_HAS_WAV = 2**16 # added to header[0] to indicate that .wav info is at the end

header = numpy.zeros(shape=(8,), dtype=numpy.int32)
header[0] = 2                  # version (4-10-09: vers 2 places click cal into DC portion)
header[1] = 32                 # nbytes in header
header[2] = 0
header[NCHANS_INDX] = 0        # num chans
header[SRATE_INDX] = 250000    # samp rate
header[BLKSZ_INDX] = NSAMPS    # block size
header[NFPTS_INDX] = NSAMPS/2  # n freq points
header[CHANS_INDX] = 0         # bit mask of chans

deltaF = 0.0

swapChDrawOrder = 0

path = ''
open_file = ''

def MakeFlatTable(maxLevel): # later, can add maxFreq arg (default=125000)

    global deltaF, path

    header[NCHANS_INDX] = 2 # 2 chan
    header[CHANS_INDX] = 3  # both chans
#    header[SRATE_INDX] =   # leave alone for now

    for iChan in range(2):
        xducer_cal_table[iChan][:] = maxLevel
    
    deltaF = float(header[SRATE_INDX]) / header[BLKSZ_INDX]

    path = ''

def OpenFile(frame):
    global deltaF, path, open_file

    wildcard = "Transducer cal files (*.trn)|*.trn|" \
       "All files (*.*)|*.*"

    dlg = wx.FileDialog(
        frame, message="Choose a file", defaultDir=frame.currPath, 
        defaultFile="", wildcard=wildcard, style=wx.OPEN
        )
    if dlg.ShowModal() != wx.ID_OK:
        return 1
    path = dlg.GetPath()
    frame.currPath = os.path.dirname(path)
    open_file = os.path.basename(path)

    f = open(path, 'rb') # read binary
    hdr = rec.array(f, formats='8i4', shape=1, names='hdr', byteorder='little')[0].hdr
    print 'hdr[0]=',hdr[0]
    hdr_vers = hdr[0] & ~VERS_HAS_WAV
    if hdr_vers != 1 and hdr_vers != 2:
        frame.WarnDlg("Unexpected file version")
        return 1
    if hdr[1] != 32 or hdr[2] != 0 or hdr[5] != NSAMPS or hdr[6] != NSAMPS/2:
        frame.WarnDlg("Unexpected header format")
        return 1

    # header looks good, store the parts that are not constant
    header[NCHANS_INDX] = hdr[NCHANS_INDX]
    header[SRATE_INDX] = hdr[SRATE_INDX]
    header[CHANS_INDX] = hdr[CHANS_INDX]

    deltaF = float(header[SRATE_INDX]) / header[BLKSZ_INDX]        

    frame.bDebViewTime = 0

    strSRate = '%d' % int(header[SRATE_INDX]/1000)
    try:
        frame.controlPanel.srate.SetValue(strSRate)
    except:
        print 'frame does not have srate in controlPanel'

    nFreqPoints = hdr[6]
    dformat = '%df4' % nFreqPoints
    if hdr[CHANS_INDX] & 1:
        print 'got something in ch1'
        xducer_cal_table[0] = rec.array(f, formats=dformat, shape=1, names='tdata', byteorder='little')[0].tdata # tdata (NOT data)

    if hdr[CHANS_INDX] & 2:
        print 'got something in ch2'
        xducer_cal_table[1] = rec.array(f, formats=dformat, shape=1, names='tdata', byteorder='little')[0].tdata # tdata (NOT data)

    if hdr[0] & VERS_HAS_WAV:
        line = f.readline().rstrip("\r\n")
        params = {}
        vglobals = {}
        try:
            exec line in vglobals, params
        except:
            self.ErrorDlg("ERROR: cannot understand .wav info")
        global wav_cal_table
        wav_cal_table = params['wav_cal_table']
        for i in range(2):
            if len(wav_cal_table[i]):
                print 'wav calibrations in chan %d:' % (i+1)
                fnames = wav_cal_table[i].keys()
                fnames.sort()
                for fn in fnames:
                    if not fn:
                        #print 'MISSING fn'
                        continue
                    wav_cal = wav_cal_table[i][fn]
                    print '%s\t%.1f' % (fn,wav_cal)

    f.close()

##    try:
##        frame.PlotCalTable()
##    except:
##        print 'frame does not have PlotCalTable'

    return 0 # good status

def ImportFile(frame, inputFormat):
    global path

    wildcard = "Transducer cal files (*.txt)|*.txt|" \
       "All files (*.*)|*.*"

    dlg = wx.FileDialog(
        frame, message="Choose a file", defaultDir=frame.currPath, 
        defaultFile="", wildcard=wildcard, style=wx.OPEN
        )
    if dlg.ShowModal() != wx.ID_OK:
        return
    path = dlg.GetPath()
    frame.currPath = os.path.dirname(path)

    nCalTableSize = NSAMPS/2
    cal_table = numpy.zeros(shape=(nCalTableSize,), dtype=numpy.float32) # local var

    chan0 = frame.controlPanel.chanToCal.GetSelection() # chan (1st is 0)
    chan1 = chan0 + 1   # chan (1st is 1)
    if chan1 == 1:
        otherChan1 = 2  # other chan (1st is 1)
    else:
        otherChan1 = 1  # other chan (1st is 1)

    if header[CHANS_INDX] & otherChan1:
        # already have cal table in memory (other chan). Use its samplerate
        freqEnd = header[SRATE_INDX] / 2.0
    else:
        # no cal table in memory OR cal table is in chan we are importing, so ignore it
        freqEnd = 1e3 * float(frame.controlPanel.maxFreq.GetValue()) # in Hz
    freqDelt = freqEnd / nCalTableSize

    f = open(path, 'rU') # read text

    if inputFormat == SANDRA:
        # read header
        # max (peak) voltage of D/A (Sys2)
        spikeMaxV = 10.0 * 32000.0/32767.0
        # "to reach	90\tdB\t@1000Hz need\t2.94754409790039\tV"
        line = f.readline().rstrip("\r\n") # chop off \n
        parts = line.split()
        if parts[0] != 'to' or parts[1] != 'reach' or parts[3] != 'dB':
            frame.WarnDlg('WARNING: unexpected header line: '+line)
        refSPL = float(parts[2])
        refV = float(parts[6])
        print 'ref SPL=',refSPL,' ref V=',refV
        f.readline() # skip line: "frequency (Hz)	factor (isoSPL)"

    freqIndxPrev = -1
    nVals = 0
    while True:
        line = f.readline().rstrip("\r\n") # chop off \n
        if not line:
            # ran out of data
            break
        strs = line.replace(',','.').split()    # in case european style decimal points; split fields
        if len(strs) == 2:
            strFreq,str_inV = strs
        else:
            # print 'ignoring line:',line
            continue
        try:
            freq = float(strFreq)
        except:
            print 'bad freq:',strFreq
            continue
        if inputFormat == SANDRA:
            VAtRefSPL = float(str_inV) * refV
            calVal = refSPL + 20.0 * math.log10(spikeMaxV/VAtRefSPL)
        else:
            # SIMPLE format
            freq *= 1e3 # kHz -> Hz
            try:
                calVal = float(str_inV)
            except:
                print 'bad spl:',str_inV
                continue
        # now store in cal table
        i = int(0.5+freq/freqDelt)
        if i >= nCalTableSize:
            # past max freq
            frame.WarnDlg('WARNING: imported file extends beyond the max freq you have specified')
            break
        cal_table[i] = calVal
        if freqIndxPrev >= 0 and i > freqIndxPrev:
            # interpolate between current point (i) and prev (freqIndxPrev)
            deltaY = (calVal - cal_table[freqIndxPrev]) / (i - freqIndxPrev) # slope
            currY = cal_table[freqIndxPrev] + deltaY
            for ii in range(freqIndxPrev+1, i, 1):
                cal_table[ii] = currY
                currY += deltaY
        freqIndxPrev = i
        nVals += 1

    if nVals <= 0:
        frame.WarnDlg('WARNING: did not find any data lines.')
        return
    else:
        frame.WarnDlg('INFO: Read in %d freq:spl pairs.' % nVals)

    xducer_cal_table[chan0] = cal_table
    header[CHANS_INDX] |= chan1

    header[SRATE_INDX] = int(freqDelt * NSAMPS)
    # update header[NCHANS_INDX]
    header[NCHANS_INDX] = 0
    if header[CHANS_INDX] & 1:
        header[NCHANS_INDX] += 1
    if header[CHANS_INDX] & 2:
        header[NCHANS_INDX] += 2

    frame.bDebViewTime = 0

    f.close()

    global deltaF
    deltaF = float(header[SRATE_INDX]) / header[BLKSZ_INDX]        

    frame.PlotCalTable()

def ExportFile(frame, outputFormat):
    print 'ExportFile: outputFormat=',outputFormat
    chan = frame.controlPanel.chanToCal.GetSelection() # 1st is 0
    chan1 = chan+1
    if not (header[CHANS_INDX] & (chan1)):
        frame.ErrorDlg('ERROR: channel %d not loaded' % chan1)
        return
    cal_table = xducer_cal_table[chan]

    wildcard = "Trans Cal file (*.txt)|*.txt|" \
       "All files (*.*)|*.*"
    dlg = wx.FileDialog(
        frame, message="Save file as ...", defaultDir=frame.currPath, 
        defaultFile=".txt", wildcard=wildcard, style=wx.SAVE+wx.OVERWRITE_PROMPT
        )
    if dlg.ShowModal() != wx.ID_OK:
        return
    outpath = dlg.GetPath()
    frame.currPath = outpath
    dlg.Destroy()

    try:
        f = open(outpath, 'w') # write text
        freqDelt = float(header[SRATE_INDX]) / float(NSAMPS)
        freqMax = float(header[SRATE_INDX]) / 2.0
        if outputFormat == ONE_KHZ:
            for freq in range(1000, int(freqMax), 1000):
                i = int(0.5+float(freq)/freqDelt)
                f.write('%g\t%g\n' % (freq*1e-3,cal_table[i]))
        else:
            for i in range(NSAMPS/2):
                freq = i * freqDelt
                f.write('%g\t%g\n' % (freq*1e-3,cal_table[i]))
        f.close()

    except IOError, (errno, strerror):
        estr = "I/O error(%s): %s." % (errno, strerror)
        if errno == 13:
            estr += ' The destination file may be open by another program.'
        frame.ErrorDlg(estr)
    except:
        frame.ErrorDlg('unknown error while storing cal file')

    return

def SaveFile(frame):
    global path

    wildcard = "Trans Cal file (*.trn)|*.trn|" \
       "All files (*.*)|*.*"
    dlg = wx.FileDialog(
        frame, message="Save file as ...", defaultDir=frame.currPath, 
        defaultFile=".trn", wildcard=wildcard, style=wx.SAVE+wx.OVERWRITE_PROMPT
        )
    if dlg.ShowModal() != wx.ID_OK:
        return
    path = dlg.GetPath()
    frame.currPath = path
    dlg.Destroy()

    try:
        f = open(path, 'wb') # write binary
        if len(wav_cal_table[0]) or len(wav_cal_table[1]):
            # write .wav cal info at the end
            header[0] |= VERS_HAS_WAV
        header.tofile(f)

        if header[CHANS_INDX] & 1:
            xducer_cal_table[0].tofile(f)
        if header[CHANS_INDX] & 2:
            xducer_cal_table[1].tofile(f)

        if header[0] & VERS_HAS_WAV:
            # write .wav cal info at the end
            f.write('wav_cal_table = ' + str(wav_cal_table) + '\n')
        f.close()

        # frame.controlPanel.SaveButt.Disable()
    except IOError, (errno, strerror):
        estr = "I/O error(%s): %s." % (errno, strerror)
        if errno == 13:
            estr += ' The destination file may be open by another program.'
        frame.ErrorDlg(estr)
    except:
        frame.ErrorDlg('unknown error while storing cal file')

    return

def Plot(frame):
    global swapChDrawOrder
    
##    print 'yo, frame.bDebug=',frame.bDebug
##    print 'yo, frame.bDebViewTime=',frame.bDebViewTime
    if not frame.bDebug:
        y_anno = 'dB SPL if no atten'
        title = 'Transducer Calibration'
    else:
        if frame.bDebViewTime:
            title = '(Debug) microphone recording'
            y_anno = 'Volts at A/D'
        else:
            title = '(Debug) microphone spectrum'
            if frame.bDeb_db_volts:
                y_anno = 'dBV at A/D'
            else:
                y_anno = 'actual dB SPL'

    anno = ['ch1','ch2']
    color = ['blue','red']
    lines = []

    if (swapChDrawOrder):
        iStart = 1
        iStop = -1
        iStep = -1
        swapChDrawOrder = 0
    else:
        iStart = 0
        iStop = 2
        iStep = 1
        swapChDrawOrder = 1

    if frame.bDebViewTime:
        # DEBUG MODE - display time domain data in time_domain_table[chan-1]
        deltaT = 1.0 / header[SRATE_INDX]
        deltaT *= 1000.0    # convert to ms
#        n = NSAMPS
        n = NSAMPS / 2
        tMax = n * deltaT

        for i in range(iStart,iStop,iStep):
            if header[CHANS_INDX] & (i+1):
                # plot chan (i+1)
                lines.append(frame.graphPanel.MakeLine(
                    0.0,deltaT,xducer_cal_table[i],n,anno[i],color[i]))
#                    0.0,deltaT,time_domain_table[i],n,anno[i],color[i]))

        frame.graphPanel.Draw(wx.lib.plot.PlotGraphics(lines, title, "Time (ms)", y_anno),
#                             xAxis=(0,tMax), yAxis= (-32768.0,32768.0))
                             xAxis=(0,tMax), yAxis= (-10.0,10.0))
    else:
        # normal
        deltaF = float(header[SRATE_INDX]) / header[BLKSZ_INDX]        
        deltaF /= 1000.0    # convert to kHz
        try:
            freqMax = float(frame.controlPanel.maxFreq.GetValue()) # in kHz
        except:
            freqMax = float(header[SRATE_INDX]) / 2.0
        n = int(0.5 + freqMax / deltaF) # number of points to display
        if n >= header[NFPTS_INDX]:
            n = header[NFPTS_INDX] - 1
            freqMax = deltaF * n

        for i in range(iStart,iStop,iStep):
            if header[CHANS_INDX] & (i+1):
                # plot chan (i+1)
                lines.append(frame.graphPanel.MakeLine(
                    deltaF,deltaF,xducer_cal_table[i][1:n],n-1,anno[i],color[i]))

        frame.graphPanel.Draw(wx.lib.plot.PlotGraphics(lines, title, "Freq (kHz)", y_anno),
                             xAxis=(0,freqMax))#, yAxis= (20,120))
