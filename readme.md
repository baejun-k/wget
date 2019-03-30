# wget

windows10의 경우 powershell에 있있으나 다른 버전에서 간단히 사용할 목적으로  
wget의 기본 기능만 사용할 수 있도록 만듦.  

멀티 프로세스 형태로 사용하려 만들었음.  

## 다운로드 진행 상태는 error stream으로 출력.  
  _"downloading: BytesReceived TotalBytesToReceive"_ 형태로 출력 되며 TotalBytesToReceive이 *-1로 나오는 경우가 있음*
  
  
## 기본적인 사용 방법.  
  ```console
  wegt.exe "Download Url" [Options]
  
  e.g.
	wget.exe file_url -O ./newdir/downloadfile -x
  ```
  
  
## Options  
### HTTP:  
* -u / --user     : The user of the credential.  
* -p / --password : The password of the credential.  
* -T / --timeout  : set the read timeout to SECONDS.  
### Dowload:  
* -O / --output-document    : write documents to FILE.  
* -x / --force-directories  : force creation of directories.  
* -P / --directory-prefix   : save files to PREFIX/...  
* -S / --string             : Output as a string.  
### -h / --help : print help.  
  
  
## 프로스세 정상 종료 후 반환 코드.  
  UNKNOWN = -1, COMPLETED = 0, CANCELLED = 1, ERROR = 2 을 반환함.
### Result Value  
* UNKNOWN = -1,  
* COMPLETED = 0,  
* CANCELLED = 1,  
* ERROR = 2
