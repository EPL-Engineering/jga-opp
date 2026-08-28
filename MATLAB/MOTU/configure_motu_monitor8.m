function configure_motu_monitor8(url)

if nargin > 0
   if contains(url, 'simulated')
      return;
   end
   
   if ~startsWith(url, 'http')
      url = ['http://' url];
   end
   baseURL = [url '/datastore/'];
else
   % EarLAB:
   baseURL = "http://169.254.228.35/datastore/";
   % Children's:
   % baseURL = "http://169.254.158.50/datastore/";
end

%% === Name USB inputs ====================================================
webwrite(baseURL + "ext/ibank/3/ch/0/name", 'json={"value":"Video 1"}');
webwrite(baseURL + "ext/ibank/3/ch/1/name", 'json={"value":"Video 2"}');
webwrite(baseURL + "ext/ibank/3/ch/2/name", 'json={"value":"Signal"}');
webwrite(baseURL + "ext/ibank/3/ch/3/name", 'json={"value":"Subject"}');
webwrite(baseURL + "ext/ibank/3/ch/4/name", 'json={"value":"TTS"}');
webwrite(baseURL + "ext/ibank/3/ch/5/name", 'json={"value":"Beacon"}');
webwrite(baseURL + "ext/ibank/3/ch/6/name", 'json={"value":"Tester Mic"}');
webwrite(baseURL + "ext/ibank/3/ch/7/name", 'json={"value":"Booth Mic"}');

%% === Name mixer outputs =================================================
webwrite(baseURL + "ext/ibank/16/ch/0/name", 'json={"value":"Caregiver Out"}');
webwrite(baseURL + "ext/ibank/16/ch/1/name", 'json={"value":"Waver Out"}');
webwrite(baseURL + "ext/ibank/16/ch/2/name", 'json={"value":"Participant Out"}');
webwrite(baseURL + "ext/ibank/16/ch/3/name", 'json={"value":"Tester Out"}');

webwrite(baseURL + "ext/obank/6/ch/0/name", 'json={"value":"AcousticPTC"}');  %
webwrite(baseURL + "ext/obank/6/ch/1/name", 'json={"value":"AcousticPTC"}');  %

%% === Name mixer inputs ==================================================
% --- Caregiver ---
webwrite(baseURL + "ext/obank/17/ch/0/name", 'json={"value":"Caregiver"}');     % In 1

% --- Waver ---
webwrite(baseURL + "ext/obank/17/ch/1/name", 'json={"value":"WaverStim"}');     % In 2
webwrite(baseURL + "ext/obank/17/ch/2/name", 'json={"value":"WaverBeacon"}');   % In 3
webwrite(baseURL + "ext/obank/17/ch/3/name", 'json={"value":"Talkback"}');      % In 4
webwrite(baseURL + "ext/obank/17/ch/4/name", 'json={"value":"WaverTTS"}');      % In 5

% --- Subject ---
webwrite(baseURL + "ext/obank/17/ch/5/name", 'json={"value":"Subject"}');       % In 6
webwrite(baseURL + "ext/obank/17/ch/6/name", 'json={"value":"Video"}');         % In 7

% --- Tester ---
webwrite(baseURL + "ext/obank/17/ch/7/name", 'json={"value":"TesterStim"}');    % In 8
webwrite(baseURL + "ext/obank/17/ch/8/name", 'json={"value":"TesterBeacon"}');  % In 9
webwrite(baseURL + "ext/obank/17/ch/9/name", 'json={"value":"Booth"}');         % In 10
webwrite(baseURL + "ext/obank/17/ch/10/name", 'json={"value":"TesterTTS"}');    % In 11

%% Connect USB inputs to mixer inputs
% --- Caregiver ---
webwrite(baseURL + "ext/obank/17/ch/0/src", 'json={"value":"3:2"}'); % Signal to In 1 (Caregiver)

% --- Waver ---
webwrite(baseURL + "ext/obank/17/ch/1/src", 'json={"value":"3:2"}'); % Signal to In 2 (WaverStim)
webwrite(baseURL + "ext/obank/17/ch/2/src", 'json={"value":"3:5"}'); % Beacon to In 3 (WaverBeacon)
webwrite(baseURL + "ext/obank/17/ch/3/src", 'json={"value":"3:6"}'); % Tester Mic To In 4 (Talkback)
webwrite(baseURL + "ext/obank/17/ch/4/src", 'json={"value":"3:4"}'); % TTS to In 5 (WaverTTS)

% --- Subject ---
webwrite(baseURL + "ext/obank/17/ch/5/src", 'json={"value":"3:3"}'); % Subject to In 6 (Subject)
webwrite(baseURL + "ext/obank/17/ch/6/src", 'json={"value":"3:0"}'); % Video to In 7 (Video)

% --- Tester ---
webwrite(baseURL + "ext/obank/17/ch/7/src", 'json={"value":"3:2"}');  % Signal to In 7 (TesterStim)
webwrite(baseURL + "ext/obank/17/ch/8/src", 'json={"value":"3:5"}');  % Beacon to In 8 (TesterBeacon)
webwrite(baseURL + "ext/obank/17/ch/9/src", 'json={"value":"3:7"}');  % Booth Mic To In 9 (Booth)
webwrite(baseURL + "ext/obank/17/ch/10/src", 'json={"value":"3:4"}'); % TTS to In 10 (TesterTTS)

%% === Connect mixer outputs to analog outputs ============================
webwrite(baseURL + "ext/obank/2/ch/0/src", 'json={"value":"16:0"}'); % A: Caregiver
webwrite(baseURL + "ext/obank/2/ch/1/src", 'json={"value":"16:0"}'); %
webwrite(baseURL + "ext/obank/3/ch/0/src", 'json={"value":"16:1"}'); % B: Waver
webwrite(baseURL + "ext/obank/3/ch/1/src", 'json={"value":"16:1"}'); %
webwrite(baseURL + "ext/obank/4/ch/0/src", 'json={"value":"16:2"}'); % C: Subject
webwrite(baseURL + "ext/obank/4/ch/1/src", 'json={"value":"16:2"}'); %
webwrite(baseURL + "ext/obank/5/ch/0/src", 'json={"value":"16:3"}'); % D: Tester
webwrite(baseURL + "ext/obank/5/ch/1/src", 'json={"value":"16:3"}'); %
webwrite(baseURL + "ext/obank/6/ch/0/src", 'json={"value":"3:0"}');  % E: AcousticPTC Subject
webwrite(baseURL + "ext/obank/6/ch/1/src", 'json={"value":"3:1"}');  %

%% === Set mix ============================================================
% --- Caregiver mixer input ---
webwrite(baseURL + "mix/chan/0/matrix/aux/0/send", 'json={"value":1}'); % CAREGIVER TO CAREGIVER OUT
webwrite(baseURL + "mix/chan/0/matrix/aux/1/send", 'json={"value":0}'); % Caregiver to Waver Out
webwrite(baseURL + "mix/chan/0/matrix/aux/2/send", 'json={"value":0}'); % Caregiver to Participant Out
webwrite(baseURL + "mix/chan/0/matrix/aux/3/send", 'json={"value":0}'); % Caregiver to Tester Out

% --- WaverStim mixer input ---
webwrite(baseURL + "mix/chan/1/matrix/aux/0/send", 'json={"value":0}'); % WaverStim to Caregiver Out
webwrite(baseURL + "mix/chan/1/matrix/aux/1/send", 'json={"value":1}'); % WAVERSTIM TO WAVER OUT
webwrite(baseURL + "mix/chan/1/matrix/aux/2/send", 'json={"value":0}'); % WaverStim to Participant Out
webwrite(baseURL + "mix/chan/1/matrix/aux/3/send", 'json={"value":0}'); % WaverStim to Tester Out

% --- WaverBeacon mixer input ---
webwrite(baseURL + "mix/chan/2/matrix/aux/0/send", 'json={"value":0}'); % WaverBeacon to Caregiver Out
webwrite(baseURL + "mix/chan/2/matrix/aux/1/send", 'json={"value":1}'); % WAVERBEACON TO WAVER OUT
webwrite(baseURL + "mix/chan/2/matrix/aux/2/send", 'json={"value":0}'); % WaverBeacon to Participant Out
webwrite(baseURL + "mix/chan/2/matrix/aux/3/send", 'json={"value":0}'); % WaverBeacon to Tester Out

% --- Talkback mixer input ---
webwrite(baseURL + "mix/chan/3/matrix/aux/0/send", 'json={"value":0}'); % Talkback to Caregiver Out
webwrite(baseURL + "mix/chan/3/matrix/aux/1/send", 'json={"value":1}'); % TALKBACK TO WAVER OUT
webwrite(baseURL + "mix/chan/3/matrix/aux/2/send", 'json={"value":0}'); % Talkback to Participant Out
webwrite(baseURL + "mix/chan/3/matrix/aux/3/send", 'json={"value":0}'); % Talkback to Tester Out

% --- WaverTTS mixer input ---
webwrite(baseURL + "mix/chan/4/matrix/aux/0/send", 'json={"value":0}'); % WaverTTS to Caregiver Out
webwrite(baseURL + "mix/chan/4/matrix/aux/1/send", 'json={"value":1}'); % WAVERTTS TO WAVER OUT
webwrite(baseURL + "mix/chan/4/matrix/aux/2/send", 'json={"value":0}'); % WaverTTS to Participant Out
webwrite(baseURL + "mix/chan/4/matrix/aux/3/send", 'json={"value":0}'); % WaverTTS to Tester Out

% --- (Subject) Stimulus mixer input
webwrite(baseURL + "mix/chan/5/matrix/aux/0/send", 'json={"value":0}'); % Subject to Caregiver Out
webwrite(baseURL + "mix/chan/5/matrix/aux/1/send", 'json={"value":0}'); % Subject to Waver Out
webwrite(baseURL + "mix/chan/5/matrix/aux/2/send", 'json={"value":1}'); % SUBJECT TO PARTICIPANT OUT
webwrite(baseURL + "mix/chan/5/matrix/aux/3/send", 'json={"value":0}'); % Subject to Tester Out

% --- (Subject) Video mixer input
webwrite(baseURL + "mix/chan/6/matrix/aux/0/send", 'json={"value":0}'); % Video to Caregiver Out
webwrite(baseURL + "mix/chan/6/matrix/aux/1/send", 'json={"value":0}'); % Video to Waver Out
webwrite(baseURL + "mix/chan/6/matrix/aux/2/send", 'json={"value":1}'); % VIDEO TO PARTICIPANT OUT
webwrite(baseURL + "mix/chan/6/matrix/aux/3/send", 'json={"value":0}'); % Video to Tester Out

% --- TesterStim mixer input ---
webwrite(baseURL + "mix/chan/7/matrix/aux/0/send", 'json={"value":0}'); % TesterStim to Caregiver Out
webwrite(baseURL + "mix/chan/7/matrix/aux/1/send", 'json={"value":0}'); % TesterStim to Waver Out
webwrite(baseURL + "mix/chan/7/matrix/aux/2/send", 'json={"value":0}'); % TesterStim to Participant Out
webwrite(baseURL + "mix/chan/7/matrix/aux/3/send", 'json={"value":1}'); % TESTERSTIM TO TESTER OUT

% --- TesterBeacon mixer input ---
webwrite(baseURL + "mix/chan/8/matrix/aux/0/send", 'json={"value":0}'); % TesterBeacon to Caregiver Out
webwrite(baseURL + "mix/chan/8/matrix/aux/1/send", 'json={"value":0}'); % TesterBeacon to Waver Out
webwrite(baseURL + "mix/chan/8/matrix/aux/2/send", 'json={"value":0}'); % TesterBeacon to Participant Out
webwrite(baseURL + "mix/chan/8/matrix/aux/3/send", 'json={"value":1}'); % TESTERBEACON TO TESTER OUT

% --- Booth mixer input ---
webwrite(baseURL + "mix/chan/9/matrix/aux/0/send", 'json={"value":0}'); % Booth to Caregiver Out
webwrite(baseURL + "mix/chan/9/matrix/aux/1/send", 'json={"value":0}'); % Booth to Waver Out
webwrite(baseURL + "mix/chan/9/matrix/aux/2/send", 'json={"value":0}'); % Booth to Participant Out
webwrite(baseURL + "mix/chan/9/matrix/aux/3/send", 'json={"value":1}'); % BOOTH TO TESTER OUT

% --- TesterTTS mixer input ---
webwrite(baseURL + "mix/chan/10/matrix/aux/0/send", 'json={"value":0}'); % TesterTTS to Caregiver Out
webwrite(baseURL + "mix/chan/10/matrix/aux/1/send", 'json={"value":0}'); % TesterTTS to Waver Out
webwrite(baseURL + "mix/chan/10/matrix/aux/2/send", 'json={"value":0}'); % TesterTTS to Participant Out
webwrite(baseURL + "mix/chan/10/matrix/aux/3/send", 'json={"value":1}'); % TESTERTTS TO TESTER OUT





