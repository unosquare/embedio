## EmbedIO CLI

Start any web folder or EmbedIO-enabled DLL from command line.

### Run web folder (static content only)

```
$ Unosquare.Labs.EmbedIO.Command -p c:\wwwroot
```

### Run web folder with WebAPI or WebSocket Assembly

```
$ Unosquare.Labs.EmbedIO.Command -p c:\wwwroot --api mywebapi.dll
```