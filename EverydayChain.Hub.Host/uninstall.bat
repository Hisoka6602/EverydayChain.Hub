set serviceName=EverydayChain.Hub.Host

sc stop   %serviceName% 
sc delete %serviceName% 

pause