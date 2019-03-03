# RDPCertInstaller

Platform | Windows 
---------|---------
Build status | [![Build status](https://ci.appveyor.com/api/projects/status/cikrnxtvx86ipn19?svg=true)](https://ci.appveyor.com/project/HumMan/rdpcertinstaller)

Utility that automates rdp certificate installing/updating on not server windows desktop

Открываем
https://zerossl.com/free-ssl/#crt

Вставляем 
for_renew_account-key.txt
for_renew_domain-csr.txt

Получаем файл подтверждения
Кладём его в .well-known

Подтверждаем
Получаем domain-crt.txt

Открываем RDPCertInstaller.exe
key - myprivate_domain.key
cert - domain-crt.txt

Install RDP cert
