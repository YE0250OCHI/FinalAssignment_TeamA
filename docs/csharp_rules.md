# C# コーディングルール

## 命名規則

``` csharp
// クラス・メソッド・プロパティ
PascalCase

// ローカル変数・引数
camelCase

// フィールド（private メンバ変数）
_camelCase
```

## コメント・改行・インデント

``` csharp
// コメントは、 // ("//" + '(半角スペース)')

// オールマン・スタイル
//インデントはスペース4つ（VisualStudio標準）
private void Hoge()
{
    /* 内部処理 */
}
```

## if・switch文

``` csharp
// { } は、省略しない
// 可能な限り早期リターン
if (taskCount < 1)
{
    return;
}

// switchは、原則多用しない
// 可能であれば、Switch式をつかう
var hoge = huga switch
{
    1 => "abc",
    2 => "def",
    _ => "hij"
};
```

## for・while

``` csharp
// できるだけ、foreachをつかう
// 回数が決まっている処理ならfor文OK

// コンソールの処理ループのみ、while可
while (true)
{
    /* ループ処理 */
}
```

## 

