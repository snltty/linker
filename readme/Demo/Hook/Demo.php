<?php
declare(strict_types=1);

namespace App\Plugin\Demo\Hook;


use App\Controller\Base\View\ManagePlugin;
use Kernel\Annotation\Hook;

use App\Util\Plugin;

class Demo extends ManagePlugin
{
    #[Hook(point: \App\Consts\Hook::USER_API_ORDER_PAY_AFTER)]
    public function tradeAfter($commodity, $order, $pay)
    {
        $lines = array_map(function($line) {
            return rtrim($line, "\r");
        }, explode("\n", $order->secret));
        $secret = json_decode($lines[0],true);

        try{
            $widget = json_decode($order->widget,true);
            foreach($widget as $k=>$v){
                $secret["Widget".$k] = $v["value"];
            }
        }catch(\Exception $e){
        }
        //file_put_contents("order.txt",json_encode($order,JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES));
        $secret["OrderId"] = "ACG".$order["trade_no"];
        $secret["Contact"] = $order["contact"];
        $secret["CostPrice"] = $order['commodity']["factory_price"];
        $secret["Price"] = $order['commodity']["price"];
        $secret["UserPrice"] = $order['commodity']["user_price"];
        $secret["PayPrice"] = $order["amount"];
        $secret["Count"] = $order["card_num"];
        $order->secret = json_encode($secret,JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
        //file_put_contents("secret.txt",$order->secret);
        $config = Plugin::getConfig("Demo");
        $aesCrypto = new AesCrypto($config["KeyId"]);
        $order->secret = base64_encode($aesCrypto->encode($order->secret));
        $order->save();
    }
}
class AesCrypto
{
    private $key;
    private $iv;
    private $password;

    public function __construct($password)
    {
        $this->password = $password;
        $this->initAes();
    }

    private function initAes()
    {
        $keyAndIV = $this->generateKeyAndIV($this->password);
        $this->key = $keyAndIV['key'];
        $this->iv = $keyAndIV['iv'];
    }

    public function encode($data)
    {
        return $this->encodeWithOffset($data, 0, strlen($data));
    }

    public function encodeWithOffset($data, $offset, $length)
    {
        $data = substr($data, $offset, $length);
        return openssl_encrypt($data, 'AES-128-CBC', $this->key, OPENSSL_RAW_DATA, $this->iv);
    }

    public function decode($data)
    {
        return $this->decodeWithOffset($data, 0, strlen($data));
    }

    public function decodeWithOffset($data, $offset, $length)
    {
        $data = substr($data, $offset, $length);
        return openssl_decrypt($data, 'AES-128-CBC', $this->key, OPENSSL_RAW_DATA, $this->iv);
    }

    private function generateKeyAndIV($password)
    {
        $hash = hash('sha384', $password, true);
        $key = substr($hash, 0, 16); // 取前 16 字节作为密钥
        $iv = substr($hash, 16, 16); // 取接下来的 16 字节作为 IV
        return ['key' => $key, 'iv' => $iv];
    }
}
