# bone_graph

## 概要
PmxEditor用のプラグインです。  
ボーンの構造を有向グラフ化し、[Graphviz](https://www.graphviz.org/)のdot形式でテキスト出力します。  
親子の他、付与親、IKターゲット、IKリンクの関係も出力します。  
サンプルのlegend.pmxを出力・変換すると、下図のようになります。  
  
![凡例](https://user-images.githubusercontent.com/16065740/62831882-cd5fca00-bc60-11e9-8f10-941c36150eab.png)

## 動作環境
* PmxEditor 0.2.5.4 64bit
* Windows 10 64bit
    * 単に私の環境です
* GraphViz 2.38.0
    * https://www.graphviz.org/ より入手可能です
    * グラフ構造を可視化するツールです

## ビルド方法
* C#コンパイラのパスを確認
    * Windows 10には素で4.xコンパイラが入っています。ビルドのバッチファイルはこれを呼ぶように書きました
        * 手元環境では以下にあり、バージョンは4.7.3190.0でした
        * `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
    * Windows 7でも PmxEditorを動作させるために追加インストールする .NET Frameworkにコンパイラも含まれるので、上記ディレクトリと似た所にあるのではないでしょうか
    * [.NET Downloads](https://www.microsoft.com/net/download/windows)や[Microsoft Build Tools 2015](https://www.microsoft.com/ja-JP/download/details.aspx?id=48159)等でも入手可能です
* build.batを編集
    * 4行目、コンパイラの設定
        * コンパイラパスを必要に応じて変えてください
        * `@set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`
    * 7行目、PmxEditorのライブラリパス設定
        * PmxEditorのLibディレクトリ名を記述してください
        * C:\直下にPmxEditorをインストールした場合は以下になります
        * `@set PMXE_LIBPATH=C:\PmxEditor_0254\Lib`
* ビルド実行、インストール
    * build.batを実行するとbone\_graph.dllが出来上がりますので、PmxEditorのプラグイン置き場へ置いてください

## 使い方
* プラグイン実行して、dot ファイルを作成する
* dotファイルをGraphvizを用いて図に変換する

### プラグイン実行
直接実行の場合は、PmxEditor のメニュー[編集(E)]-[プラグインDLLの直接実行(G)]で表示される下図ダイアログボックスでbone\_graph.dllを指定し、[実行]ボタンを押下します。  
![プラグイン実行](https://user-images.githubusercontent.com/16065740/62831878-a608fd00-bc60-11e9-9af5-9219a1ef125e.PNG)  
「名前を付けて保存」のダイアログが出ますので、ファイル名を指定し、「保存」を押下します。  
プラグインは指定された名称でテキストファイルを作成し、dot形式でグラフ情報を保存します。

### 図に変換
Graphvizはsvgやpdfのようなベクトル形式にも、pngやjpgのようなラスター形式にも対応しています。  
詳細は上記のGraphvizサイトを参照してください。

"legend.dot" を変換する例を示します。

* PNG形式へ変換する例
    * `dot -Tpng legend.dot -o legend.png`

* SVG形式へ変換する例
    * `dot -Tsvg -legend.dot -o legend.svg`

## 図の説明
概要で示した図が凡例となります。  
色や形の設定はソースコードの始めの方で記述しているので、必要に応じて書き換えてください。


以上、何かのお役に立てば。