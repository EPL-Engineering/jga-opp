baseURL = "http://169.254.128.32/datastore/";

% Name USB inputs
webwrite(baseURL + "ext/ibank/3/ch/0/name", 'json={"value":"Video 1"}');
webwrite(baseURL + "ext/ibank/3/ch/1/name", 'json={"value":"Video 2"}');
webwrite(baseURL + "ext/ibank/3/ch/2/name", 'json={"value":"Caregiver In"}');
webwrite(baseURL + "ext/ibank/3/ch/3/name", 'json={"value":"Waver In"}');
webwrite(baseURL + "ext/ibank/3/ch/4/name", 'json={"value":"Participant In"}');
webwrite(baseURL + "ext/ibank/3/ch/5/name", 'json={"value":"Mic In"}');

% Name mixer outputs;
webwrite(baseURL + "ext/ibank/16/ch/0/name", 'json={"value":"Caregiver Out"}');
webwrite(baseURL + "ext/ibank/16/ch/1/name", 'json={"value":"Waver Out"}');
webwrite(baseURL + "ext/ibank/16/ch/2/name", 'json={"value":"Participant Out"}');

% Connect USB inputs to mixer inputs
webwrite(baseURL + "ext/obank/9/ch/0/src", 'json={"value":"3:2"}'); % Caregiver In to In 1
webwrite(baseURL + "ext/obank/9/ch/1/src", 'json={"value":"3:3"}'); % Waver In to In 2
webwrite(baseURL + "ext/obank/9/ch/2/src", 'json={"value":"3:5"}'); % Mic In to In 3
webwrite(baseURL + "ext/obank/9/ch/3/src", 'json={"value":"3:4"}'); % Participant In to In 4
webwrite(baseURL + "ext/obank/9/ch/4/src", 'json={"value":"3:0"}'); % Video 1 to In 4

% Connect mixer outputs to analog outputs
webwrite(baseURL + "ext/obank/0/ch/0/src", 'json={"value":"16:0"}');
webwrite(baseURL + "ext/obank/0/ch/1/src", 'json={"value":"16:0"}');
webwrite(baseURL + "ext/obank/0/ch/2/src", 'json={"value":"16:1"}');
webwrite(baseURL + "ext/obank/0/ch/3/src", 'json={"value":"16:1"}');
webwrite(baseURL + "ext/obank/0/ch/4/src", 'json={"value":"16:2"}');

% Set mix
webwrite(baseURL + "mix/chan/0/matrix/aux/0/send", 'json={"value":1}'); % Caregiver In to Caregiver Out
webwrite(baseURL + "mix/chan/0/matrix/aux/1/send", 'json={"value":0}'); % Caregiver In to Waver Out
webwrite(baseURL + "mix/chan/0/matrix/aux/2/send", 'json={"value":0}'); % Caregiver In to Participant Out

webwrite(baseURL + "mix/chan/1/matrix/aux/0/send", 'json={"value":0}'); % Waver In to Caregiver Out
webwrite(baseURL + "mix/chan/1/matrix/aux/1/send", 'json={"value":1}'); % Waver In to Waver Out
webwrite(baseURL + "mix/chan/1/matrix/aux/2/send", 'json={"value":0}'); % Waver In to Participant Out

webwrite(baseURL + "mix/chan/2/matrix/aux/0/send", 'json={"value":0}'); % Mic In to Caregiver Out
webwrite(baseURL + "mix/chan/2/matrix/aux/1/send", 'json={"value":1}'); % Mic In to Waver Out
webwrite(baseURL + "mix/chan/2/matrix/aux/2/send", 'json={"value":0}'); % Mic In to Participant Out

webwrite(baseURL + "mix/chan/3/matrix/aux/0/send", 'json={"value":0}'); % Participant In to Caregiver Out
webwrite(baseURL + "mix/chan/3/matrix/aux/1/send", 'json={"value":0}'); % Participant In to Waver Out
webwrite(baseURL + "mix/chan/3/matrix/aux/2/send", 'json={"value":1}'); % Participant In to Participant Out

webwrite(baseURL + "mix/chan/4/matrix/aux/0/send", 'json={"value":0}'); % Video 1 to Caregiver Out
webwrite(baseURL + "mix/chan/4/matrix/aux/1/send", 'json={"value":0}'); % Video 1 to Waver Out
webwrite(baseURL + "mix/chan/4/matrix/aux/2/send", 'json={"value":1}'); % Video 1 to Participant Out

