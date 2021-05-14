# WinDivertNAT
WinDivertNAT は、WinDivert を使用して簡易的なネットワークアドレス変換(NAT)を実施するコンソールアプリケーションです。
[WinDivert フィルタ言語](https://reqrypt.org/windivert-doc.html#filter_language)で指定したパケットに対し、下記のような操作を実施できます。
 - 送信元/送信先の IP アドレスの変更
 - 送信元/送信先の TCP/UDP ポート番号の変更
 - パケットの破棄
 - ログ(動作確認用)

## インストール
**警告：WinDivertNAT を構成するファイルは管理者権限が無い限り書き換えられないようにしてください。標準ユーザーが書き換えられるようになっている場合、権限昇格の脆弱性になります。**

1. WinDivertNAT のバイナリを PC 内に配置してください。
1. WinDivertNAT の実行可能ファイルと同じ場所に、WinDivert 2.X のバイナリファイルを配置してください。お使いの Windows が 32-bit であれば、WinDivert.dll (32-bit 版)と WinDivert32.sys を配置してください。64-bit の場合は、WinDivert.dll (64-bit 版)と WinDivert64.sys を配置してください。[WinDivert のダウンロードはこちら。](https://reqrypt.org/windivert.html)

## アンインストール
1. PC を再起動してください。これにより WinDivert ドライバーが自動的にアンインストールされます。
1. WinDivertNAT をフォルダごと削除してください。

## 使い方
WinDivertNAT を起動すると、指定されたルールに基づいてパケット操作を開始します。
ルールはコマンドライン引数で指定します。
パケット操作は WinDivertNAT が起動している限り実施されます。WinDivertNAT を CTRL-C 等で終了すると、パケット操作も同時に終了します。
複数のルールを同時に適用したい場合は、WinDivertNAT を複数起動することで実現できます。

### コマンドライン引数
#### パケット指定
これらのコマンドライン引数は、操作対象のパケットを指定します。

 - `-f`, `--filter=VALUE`：[WinDivert フィルタ言語](https://reqrypt.org/windivert-doc.html#filter_language)で指定されるパケットフィルタ文字列。
 - `-p`, `--priority=VALUE`：WinDivert ハンドルの優先度。WinDivert を使用するアプリケーションが複数ある場合に、その処理順序を指定します。優先度を高くすると、他のアプリケーションより先にパケット操作を実施します。優先度を低くすると、他のアプリケーションが操作した後のパケットを操作することになります。

#### キュー
これらのコマンドライン引数は、パケットを保持するキューのパラメータを操作します。負荷が大きすぎてパケットの取りこぼしが発生する場合は、これらのパラメータをより大きな値にすることで改善するかもしれません。

 - `--queue-length=VALUE`：キューに保持されるパケットの最大数。
 - `--queue-time=VALUE`：キューにパケットが保持される期間。
 - `--queue-size=VALUE`：キューのサイズ。単位はバイト。

#### バッファ
これらのコマンドライン引数は、WinDivertNAT が一度に操作するパケットの量を指定します。これらのパラメータを変更することで、スループットが改善する場合があります。

 - `--buf-length=VALUE`：一度に操作するパケットの最大数。
 - `--buf-size=VALUE`：一度に操作するパケットの合計サイズ最大値。単位はバイト。

#### パケット操作
これらのコマンドライン引数は、パケットの操作内容を指定します。

 - `--drop`：パケットを破棄します。
 - `--direction=VALUE`：パケットの方向を変更します。`outbound` だと PC から出ていくパケットになり、`inbound` だと PC に入ってくるパケットになります。
 - `--ifidx=VALUE`：パケットが属するインターフェイスを番号で指定します。番号は、例えば Powershell の `Get-NetIPInterface` で確認できます。
 - `--subifidx=VALUE`：パケットが属するサブ-インターフェイスを番号で指定します。
 - `--ipv4-src-addr=VALUE`：パケットが IPv4 パケットである場合に、送信元アドレスを指定されたものに書き換えます。
 - `--ipv4-dst-addr=VALUE`：パケットが IPv4 パケットである場合に、送信先アドレスを指定されたものに書き換えます。
 - `--ipv6-src-addr=VALUE`：パケットが IPv6 パケットである場合に、送信元アドレスを指定されたものに書き換えます。
 - `--ipv6-dst-addr=VALUE`：パケットが IPv6 パケットである場合に、送信先アドレスを指定されたものに書き換えます。
 - `--tcp-src-port=VALUE`：パケットに TCP パケットが含まれている場合に、送信元ポートを指定されたものに書き換えます。
 - `--tcp-dst-port=VALUE`：パケットに TCP パケットが含まれている場合に、送信先ポートを指定されたものに書き換えます。
 - `--udp-src-port=VALUE`：パケットに UDP パケットが含まれている場合に、送信元ポートを指定されたものに書き換えます。
 - `--udp-dst-port=VALUE`：パケットに UDP パケットが含まれている場合に、送信先ポートを指定されたものに書き換えます。
 - `-l`, `--log`：パケットの概要を標準出力に書き出します。有効にすると性能が著しく低下するため、動作確認時にのみ使用し、通常は無効化することが推奨されます。

#### その他
 - `-h`, `-?`, `--help`：ヘルプを表示します。

## 注意点
 - WinDivertNAT はコネクション追跡を実施しません。つまり、フィルタに適合したパケットには NAT を実施しますが、そのパケットに対する応答に逆方向の NAT を実施することはありません。応答パケットを受信したい場合は、手動で逆方向の NAT を設定する必要があります。

## コマンドの例
### DNS サーバーの変更
8.8.8.8 への DNS over UDP 通信を 1.1.1.1 へとリダイレクトするには、下記の2コマンドを同時に実行します。

```
WinDivertNAT -f "ip.DstAddr == 8.8.8.8 and udp.DstPort == 53 and outbound" --ipv4-dst-addr=1.1.1.1
WinDivertNAT -f "ip.SrcAddr == 1.1.1.1 and udp.SrcPort == 53 and inbound" --ipv4-src-addr=8.8.8.8
```

1番目のコマンドは、DNS リクエストを含むパケットに DNAT を実施します。
2番目のコマンドは、DNS レスポンスを含むパケットに SNAT を実施します。

注意：2番目のコマンドを実行すると、アドレス 1.1.1.1 を使用して 1.1.1.1 のサーバーと通信することはできなくなります。
1.1.1.1 へと送信したリクエストに対して、8.8.8.8 からレスポンスが返ってきたように見えるので、クライアントは困惑し、そのレスポンスパケットを破棄します。

### ローカルホストへのリダイレクト
198.51.100.1:52149 への TCP コネクションを 127.0.0.1:52150 へとリダイレクトするには、下記の2コマンドを同時に実行します。

```
WinDivertNAT -f "ip.DstAddr == 198.51.100.1 and tcp.DstPort == 52149 and outbound" --ipv4-dst-addr=127.0.0.1 --tcp-dst-port=52150
WinDivertNAT -f "tcp.SrcPort == 52150 and loopback" --ipv4-src-addr=198.51.100.1 --tcp-src-port=52149
```

注意：2番目のコマンドで、フィルターに ` and inbound` を追記すると動作しなくなります。これは、WinDivert にてループバックパケットが Outbound のみであると解釈されており、Inbound パスでループバックパケットを捕捉できないためです。

## ライセンス
WinDivertNAT is dual-licensed under your choice of the GNU Lesser General Public License (LGPL) Version 3 or the GNU General Public License (GPL) Version 2.
See the LICENSE file for more information.
