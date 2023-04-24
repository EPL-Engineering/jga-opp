"""

phys.py - code that can be used by all physiology software

To execute doctests:
1. Start Windows command prompt
2. Z:
3. cd dev\py-module-dev\phys
4. python phys.py (To see full output: python phys.py -v)

To develop more doctests:
1. Open this file in IDLE
2. Hit F5
3. From Shell window, call function to test, like: str_to_list('a,b')
"""


import wx
import os
import fnmatch
import time
import math

# universal functions

def ErrorDlg(parent, str):
    dlg = wx.MessageDialog(parent, str, 'Error', wx.OK | wx.ICON_INFORMATION)
    dlg.ShowModal()
    dlg.Destroy()

def WarnDlg(parent, str):
    dlg = wx.MessageDialog(parent, str, 'Warning', wx.OK | wx.ICON_INFORMATION)
    dlg.ShowModal()
    dlg.Destroy()

def InfoDlg(parent, str):
    dlg = wx.MessageDialog(parent, str, 'Message', wx.OK | wx.ICON_INFORMATION)
    dlg.ShowModal()
    dlg.Destroy()

def MsgDlg(parent, title, s, is_error=False):
##    dlg = wx.MessageDialog(parent, s, title, wx.OK | wx.ICON_INFORMATION)
    if is_error:
        dlg = wx.MessageDialog(parent, s, title, wx.OK | wx.ICON_ERROR)
        dlg.SetBackgroundColour(wx.Colour(red=255)) # does nothing?
    else:
        dlg = wx.MessageDialog(parent, s, title, wx.OK | wx.ICON_INFORMATION)
    dlg.ShowModal()
    dlg.Destroy()

def OKCancelDlg(parent, title, s, is_error=False):
    # returns wx.ID_OK or wx.ID_CANCEL
##    dlg = wx.MessageDialog(parent, s, title, wx.OK | wx.CANCEL | wx.ICON_INFORMATION)
    if is_error:
        dlg = wx.MessageDialog(parent, s, title, wx.OK | wx.CANCEL | wx.ICON_ERROR)
        dlg.SetBackgroundColour(wx.Colour(red=255)) # does nothing?
    else:
        dlg = wx.MessageDialog(parent, s, title, wx.OK | wx.CANCEL | wx.ICON_INFORMATION)
    ret_val = dlg.ShowModal()
    dlg.Destroy()
    return ret_val

def YesNoDlg(parent, title, s):
    # returns wx.ID_YES or wx.ID_NO wx.ID_CANCEL
    dlg = wx.MessageDialog(parent, s, title, wx.YES_NO | wx.CANCEL | wx.ICON_INFORMATION)
    ret_val = dlg.ShowModal()
    dlg.Destroy()
    return ret_val

def all_files(root, patterns='*', single_level=False, yield_folders=False):
    # from Python Cookbook, p.89
    patterns = patterns.split(';')
    ##print 'in all_files, root=',root
    for path, subdirs, files in os.walk(root):
        if yield_folders:
            files.extend(subdirs)
        files.sort()
        for name in files:
            for pattern in patterns:
                if fnmatch.fnmatch(name, pattern):
                    yield os.path.join(path, name)
                    break
        if single_level:
            break

def MeanSdMinMax(N, fArray):
    """ returns fMean, strSD, fMin, fMax

    >>> MeanSdMinMax(1, [2.0,])
    (2.0, 'NA', 2.0, 2.0)
    >>> MeanSdMinMax(2, [2.0,3.0])
    (2.5, '0.7', 2.0, 3.0)
    >>> MeanSdMinMax(3, [2.0,4.0,3.0])
    (3.0, '1.0', 2.0, 4.0)
    >>>
    """
    fSum = 0.0
    fSumSq = 0.0
    fMin = 1e20
    fMax = -1e20
    for i in range(N):
        val = fArray[i]
        if val > fMax:
            fMax = val
        if val < fMin:
            fMin = val
        fSum += val
    fMean = fSum / N

    if N == 1:
        strSD = "NA"
    else:
        for i in range(N):
            val = fArray[i]
            val -= fMean
            fSumSq += val*val
        fSumSq /= N-1
        strSD = "%.1f" % math.sqrt(fSumSq)
    return fMean, strSD, fMin, fMax

def lcm(m, n):
    """ a very simple, but slow, LCM routine (OK for the small numbers we'll be using)

    >>> lcm(1,5)
    5
    >>> lcm(12,15)
    60
    >>> lcm(4,6)
    12
    >>> lcm(3,4)
    12
    """
    if n > m:
        temp = m
        m = n
        n = temp
    # now m is >= n (only needed for speed)
    mn = m * n
    for i in range(m, mn+m, m):
        for j in range(n, mn+n, n):
            if i==j:
                return i
    return 0 # error

def BlockedRandom(bRanDon, n, nRanDone): # return i,nRanDone
    i = random.randint(0, n-1) # i will be 0, n-1, or value inbetween
    # got a random integer. Can we use it?

    if nRanDone >= n:
        bRanDon[:] = False
        nRanDone = 0

    while bRanDon[i]:
        i += 1
        if i == n:
            i = 0
    bRanDon[i] = True
    nRanDone += 1

    return i,nRanDone   # the random number, n random nums used

def nice_sleep(time_to_wake):
    while time.clock() < time_to_wake:
        time.sleep(0.1)
        wx.Yield()

def str_to_list(string):
    """ Returns a list. Input is a string delimited by commas or whitespace.

    >>> str_to_list('')
    []
    >>> str_to_list(' ')
    []
    >>> str_to_list('a')
    ['a']
    >>> str_to_list(' a ')
    ['a']
    >>> str_to_list('a,b')
    ['a', 'b']
    >>> str_to_list('a, b')
    ['a', 'b']
    >>> str_to_list(' a , b ')
    ['a', 'b']
    >>> str_to_list(' a  b ')
    ['a', 'b']
    >>> str_to_list(' a\tb ')
    ['a', 'b']
    >>> str_to_list(' a\t,b ')
    ['a', 'b']
    >>> str_to_list(' a b c')
    ['a', 'b', 'c']
    >>> str_to_list(' a, b, c')
    ['a', 'b', 'c']
    >>>
    """
    ret_list = []
    items = string.split(',')
    if len(items) == 0:
        # nothing there
        return []
    if len(items) == 1:
        # try another delimiter
        items = string.split()
        # if len(items) is still 1, we only have 1 item, with no delimiters. no problem
    for s in items:
        s2 = s.strip()
        if s2:
            ret_list.append(s2)
    return ret_list

def wav_fn_str_to_list(string):
    """ Returns a list of .wav file names. Input is a string delimited by commas or whitespace.

    Will append '.wav' if missing.

    >>> wav_fn_str_to_list('')
    []
    >>> wav_fn_str_to_list(' ')
    []
    >>> wav_fn_str_to_list('a.wav')
    ['a.wav']
    >>> wav_fn_str_to_list(' a.wav ')
    ['a.wav']
    >>> wav_fn_str_to_list('a.wav,b.wav')
    ['a.wav', 'b.wav']
    >>> wav_fn_str_to_list('a.wav, b.wav')
    ['a.wav', 'b.wav']
    >>> wav_fn_str_to_list(' a.wav , b.wav ')
    ['a.wav', 'b.wav']
    >>> wav_fn_str_to_list(' a.wav  b.wav ')
    ['a.wav', 'b.wav']
    >>> wav_fn_str_to_list(' a.wav\tb.wav ')
    ['a.wav', 'b.wav']
    >>> wav_fn_str_to_list(' a.wav\t,b.wav ')
    ['a.wav', 'b.wav']
    >>> wav_fn_str_to_list(' a b ')
    ['a.wav', 'b.wav']
    >>> wav_fn_str_to_list(' a b c')
    ['a.wav', 'b.wav', 'c.wav']
    >>> wav_fn_str_to_list(' a, b, c')
    ['a.wav', 'b.wav', 'c.wav']
    >>>
    """
    ret_list = []
    items = string.split(',')
    if len(items) == 0:
        # nothing there
        return []
    if len(items) == 1:
        # try another delimiter
        items = string.split()
        # if len(items) is still 1, we only have 1 item, with no delimiters. no problem
    for s in items:
        s2 = s.strip()
        if s2:
            # add .wav if missing
            if s2[-4:].lower() != '.wav':
                s2 += '.wav'
            ret_list.append(s2)
    return ret_list

def fn_str_to_list(string):
    """ Returns a list of file names. Input is a string delimited by commas or whitespace.
    """
    ret_list = []
    items = string.split(',')
    if len(items) == 0:
        # nothing there
        return []
    if len(items) == 1:
        # try another delimiter
        items = string.split()
        # if len(items) is still 1, we only have 1 item, with no delimiters. no problem
    for s in items:
        s2 = s.strip()
        if s2:
            ret_list.append(s2)
    return ret_list

if __name__ == "__main__":
    import doctest
    doctest.testmod()
    
