baseURL = "http://169.254.128.32/datastore/";

% result = webread('http://169.254.128.32/datastore/mix/chan/0');
% disp(result);

webread(baseURL + "mix/chan/0")
% webwrite(baseURL + "mix/chan/0/matrix/fader", 'json={"value":1.0}');
% webwrite(baseURL + "mix/chan/0/matrix/mute", 'json={"value":1.0}');
