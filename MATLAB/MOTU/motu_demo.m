result = webread('http://169.254.128.32/datastore/mix/chan/0');
disp(result);

result = webwrite('http://169.254.128.32/datastore/mix/chan/1/matrix/aux/0/send', 'json={"value":0.01}');