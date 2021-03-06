・CM3D2.MaidOyako.Plugin 1.0.1.0

・概要
　VR夜伽時にメイドさんとハンドコントローラーを親子付けするプラグインです。
　イリュージョンのゲームでよく話題になっているアレです。
　CM3D2/COM3D2両対応です。
　Viveで動作確認していますが、オキュラスでの動作も考慮して作ったつもりです。
　ただし、オキュラスは持っていないので動けば良いな位で…
　
　ハンドコントローラーは片方を親子付けに、もう片方を操作用に使うので、
　左右2つ必須です。
　Viveトラッカーは未対応です。(こちらも持っていないので)

・使用方法
　「UnityInjector」フォルダに「CM3D2.MaidOyako.Plugin.dll」を入れてください。
　
　夜伽時にメイドさんが居る状態で、Viveは「グリップ」をダブルクリックの要領で
　押してください。
　タッチでは「中指トリガー」を使ってください。
　(キーはコンフィグファイルを書き換えると、変更可能です)
　押した側とは別のハンドコントローラーにメイドさんが追従する様になります。
　メイドさんが複数いる場合は、親子付けする側のコントローラーに近いメイドさん
　を親子付けします。
　親子付けを解除する場合は、再度同じボタンを押してください。
　
　一旦親子付けすると、メイドさんを掴んで動かせるようになります。
　リアルでの空気嫁などに合わせて位置を調整してください。
　夜伽コマンドでモーションが変わるとズレるので、その場合も手動で調整してください。
　
　空気嫁に固定した際の誤入力を防ぐために、親子付け中のコントローラーの入力は
　受け付けない様にしています。

・コンフィグ
　初回起動時に「UnityInjector\Config」フォルダに「maidoyako.xml」が作られます。
　「sceneNameList」は、親子付けを有効にするシーン名を記述しています。
　デフォルトでは夜伽中のみ有効な設定にしていますが、スタジオモードやVIPでも
　有効にしたい場合は、下記の要素を追加してください。
    <string>ScenePhotoMode</string> <!-- 撮影モード -->
    <string>SceneFreeModeSelect</string> <!-- 回想モードVIP -->
    <string>SceneADV</string> <!-- VIP -->
　
　「startButton」で親子付けを開始するボタンの組み合わせを指定可能です。
　ボタンで指定可能な文字列は「AVRControllerButtons.BTN」で定義されているこれら8つです。
　　VIRTUAL_MENU, VIRTUAL_L_CLICK, VIRTUAL_R_CLICK, VIRTUAL_GRUB, MENU, TRIGGER, STICK_PAD, GRIP
　これらを半角の「+」で連結する事で、2ボタンまで指定可能です。
　同じボタンの場合は、ダブルクリック、違うボタンの場合はボタン1を押しながら
　ボタン2を押す指定になります。
　1ボタンだけの指定も可能ですが、被りまくるのでお勧めはしません。
　
　「1.0.0.0」と「1.0.1.0」でのデフォルト設定を記載しておきますので、
　「1.0.0.0」版の「グリップ＋トリガー」の方がよかったという場合はコピペして
　使ってください。
    <startButton>GRIP + TRIGGER</startButton> <!-- 1.0.0.0 でのデフォルト グリップ＋トリガー -->
    <startButton>GRIP + GRIP</startButton> <!-- 1.0.1.0 でのデフォルト グリップをダブルクリック -->
　
　※このプラグインにはキーコンフィグのUIは無く、直接コンフィグファイルを
　　書き換える必要があります。

・実践動画について
　ちょっとしたネタ動画です。
　バーチャルアバタースタジオの動画、もっと増えて欲しいんですけどね。
　因みに無垢ちゃんの左手は空気嫁に固定されているのであんな感じになってます。

・ソースについて
　改変、転載、ご自由にどうぞ。
　複数ファイルですので、AutoCompileに「CM3D2.MaidOyako.Plugin」フォルダ毎
　入れて貰えればコンパイル出来ます。

・変更履歴
　1.0.1.0
　　ぽろりプラグインとキーが被った為に、デフォルトのキーを変更しました。
　　キーコンフィグを追加しました。
　
　1.0.0.0
　　初版

※MODはKISSサポート対象外です。
※MODを利用するに当たり、問題が発生してもKISSは一切の責任を負いかねます。
※カスタムメイド3D2を購入されている方のみが利用できます。
※カスタムメイド3D2上で表示する目的以外の利用は禁止します。
※これらの事項は http://kisskiss.tv/kiss/diary.php?no=558 を優先します。
