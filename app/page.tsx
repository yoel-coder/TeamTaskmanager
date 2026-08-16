import TaskBoard from "../components/TaskBoard";
import { requireChatGPTUser } from "./chatgpt-auth";
export const dynamic="force-dynamic";
export default async function Home(){const user=await requireChatGPTUser("/");return <TaskBoard name={user.displayName}/>}
