Our.Umbraco.SilentUprade
------------------------
Say goodbye to upgrade screens (if you want to)

!!
!! Remember you have to turn silent upgrades on in the web.config 
!!
!! Add the following to the AppSettings section in web.config
!! 
!!  <add key="SilentUpgrade" value="true" />
!!

Silent Upgrades will attempt to run everytime you upgrade your 
umbraco installation. If the process fails, It will fallback 
to the standard installation screen.


