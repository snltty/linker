<?php
declare(strict_types=1);

namespace App\Plugin\Demo\Controller;

use App\Controller\Base\API\UserPlugin;
use App\Interceptor\Waf;
use App\Entity\CreateObjectEntity;
use App\Entity\DeleteBatchEntity;
use App\Entity\QueryTemplateEntity;
use App\Service\Query;
use App\Util\Date;
use Illuminate\Database\Eloquent\Relations\Relation;
use Kernel\Annotation\Inject;
use Kernel\Annotation\Interceptor;
use Kernel\Exception\JSONException;
use App\Model\Card;
use App\Model\Commodity;

class Api extends UserPlugin
{
    #[Inject]
    private Query $query;
    public function check():string {
        $num = (int)($_REQUEST['num'] ?? 10);
        $key = $_REQUEST['key'];
        if ($key != getPluginConfig("Demo")['KeyId']) {
            return "fail";
        }

        $list = Commodity::query()->where("status", 1)->get();
        foreach($list as $item)
        {
            try{
                $delivery_message = json_decode($item["delivery_message"],true);
                $commodity_id = $item["id"];
                $count = Card::query()->where("commodity_id", (int)$commodity_id)->where("status", 0)->count();
                if($count < $num)
                {
                    for($i = 0; $i < $num; $i++)
                    {
                        $cardObj = new \App\Model\Card();
                        $cardObj->commodity_id = $commodity_id;
                        $cardObj->owner = 0;   
                        $cardObj->secret = json_encode($delivery_message,JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
                        $cardObj->create_time = Date::current();
                        $cardObj->save();
                    }
                }
            }catch(\Exception $e){
            }
        }

        return "ok";
    }

}