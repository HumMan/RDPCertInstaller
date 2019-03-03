# RDPCertInstaller
<p align="center">
  <a href="README.md">English</a> |
  <span>Pусский</span>
</p>

Platform | Windows 
---------|---------
Build status | [![Build status](https://ci.appveyor.com/api/projects/status/cikrnxtvx86ipn19?svg=true)](https://ci.appveyor.com/project/HumMan/rdpcertinstaller)

Утилита автоматизирует обновление RDP сертификата на windows 10 (возможно работает на других версиях).
Ниже приводится описание настройки RDP для системы находящейся за роутером, с белым динамическим ip, домен для доступа задан через no-ip.com вида myhomehost.ddns.net

## Проблема
Т.к. для назначения/обновления сертификата на настольной windows нужно совершить [несколько нетривиальных действий](https://support.microsoft.com/ru-ru/help/2001849/how-to-force-remote-desktop-services-on-windows-7-to-use-a-custom-serv), была создана данная программа.

## Как пользоваться

Генерировать сертификаты будем с помощью сервиса https://zerossl.com
* Открываем страницу для создания/обновления сертификата https://zerossl.com/free-ssl/#crt
* Если мы создаём новый сертификат, то оставляем поля `account-key.txt` и `domain-csr.txt` пустыми. Файлы `account-key.txt, domain-csr.txt, myprivate_domain.key` будут сгенерированы и на следующем шаге их нужно сохранить
** файл myprivate_domain.key далее используется только локально для генерации pfx сертификата
* Если мы обновляем сертификат, то вставляем содержимое файлов, полученных при первоначальном создании сертификата, в соответствующие поля 
  * account-key.txt
  * domain-csr.txt
* Далее нам предложат подтвердить владение доменом
  * Скачиваем файл подтверждения
  * Кладём его в WebServer\wwwroot\.well-known\acme-challenge\
  * В файле WebServer\Properties\launchSettings.json указываем локальный ip системы, например  `"applicationUrl": "http://192.168.1.41:5000/"`
  * Запускаем `WebServer\run.bat`
  * Заходим в настройки роутера и пробрасываем порт 80 к 192.168.1.41:5000
  * Проверяем что WebServer работает и порт проброшен. Для этого открываем на шаге `2 - Verification` ссылку на файл, он должен отобразить содержимое  
  * Нажимаем Далее/Next
* После успешного подтверждения нам предложать скачать и сохранить файл `domain-crt.txt`
* Выключаем WebServer, удаляем проброс портов
* Запускаем `RDPCertInstaller.exe` (нужны права администратора, т.к. измеяется системная часть реестра) 
* Указываем следующие файлы
  * В поле Key - файл `myprivate_domain.key`
  * В поле Cert - файл `domain-crt.txt`
* Нажимаем Install RDP cert
* Если в конце Success значит всё успешно обновилось
* Пытаемся подключиться по RDP - сверху должен быть значёк защищенного подключения с нашим сертификатом

## Полезные ссылки по настройке/созданию сертификата RDP
https://support.microsoft.com/ru-ru/help/2001849/how-to-force-remote-desktop-services-on-windows-7-to-use-a-custom-serv
http://www.sherweb.com/blog/when-given-crt-and-key-files-make-a-pfx-file/

## Полезные ссылки по настройке RDP
https://alexmdv.ru/zashhita-rdp-podklyucheniya
http://macrodmin.ru/2016/03/secure-rdp/
