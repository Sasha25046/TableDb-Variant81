killall -9 Xvfb fluxbox x11vnc websockify dotnet 2>/dev/null || true
rm -f /tmp/.X1-lock /tmp/.X11-unix/X1
sleep 1

Xvfb :1 -screen 0 1280x800x24 &
export DISPLAY=:1
fluxbox &
x11vnc -display :1 -nopw -listen localhost -xkb -forever &
websockify --web /usr/share/novnc/ 6080 localhost:5900 &

DISPLAY=:1 dotnet run --project TableDbDesktop