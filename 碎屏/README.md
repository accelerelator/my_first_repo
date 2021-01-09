# unity碎屏后处理shader

这是我自己git仓库的第一份正式提交，姑且带点仪式感，记录一下。

先放参考链接

https://www.cnblogs.com/wantnon/p/4542172.html

https://blog.csdn.net/qq_36383623/article/details/86304894

调用：break.cs挂在相机上，新建一个 material，shader选成broken_without_lighting.shader对应的路径，把这个mat拖进break.cs的参数里。

主要是实现类似碎屏或玻璃的效果，由于用的Graphics.Blit(source, destination, mat)，所以物体本身的属性还得靠物体上的shader。另外这个shader的主贴图似乎没啥用（还没想通



