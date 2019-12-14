# 概要

[動画](https://twitter.com/nidokota/status/1205808967705718784)

UnitySimpleQuickMenuはUnityで簡単にメニューを作成できるAssetです。  
まだ開発途中ですが、簡単なメニューを作ることはできます。

# 使用方法
サンプルシーンはAssets/Scenes/SimpleQuickMenu.sceneにあります。 

Escapeキーでメニューを表示したり、前の項目に戻ります。  
上下の方向キーで項目を移動、Spaceキーで決定します。  

SimpleQuickMenu.MenuHierarchyに登録されたGameObjectの階層をメニューとして表示します。  
メニュー項目を決定した際は、その階層のSimpleQuickMenuLineに登録されたUnityEventが実行されます。  

# Unityのバージョン
現在Unity2019.2.14f1で作成しています。

# ライセンス
[MIT License](https://github.com/NidoKota/UnitySimpleQuickMenu/blob/master/LICENSE)
