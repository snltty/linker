import { sendWebsocketMsg } from './request'


export const getPlans = (machineId,category) => {
    return sendWebsocketMsg('plan/get', {machineId,category});
}
export const addPlan = (machineId,plan) => {
    return sendWebsocketMsg('plan/add', {machineId,plan});
}
export const removePlan = (machineId,planId) => {
    return sendWebsocketMsg('plan/remove', {machineId,planId});
}