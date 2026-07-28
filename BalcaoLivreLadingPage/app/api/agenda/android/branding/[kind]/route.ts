import {
  agendaAndroidErrorResponse,
  getAgendaAndroidBrandingAsset,
} from "../../../../../lib/agenda-android-server";

type RouteContext = { params: Promise<{ kind: string }> };

export async function GET(request: Request, context: RouteContext) {
  try {
    const { kind } = await context.params;
    return await getAgendaAndroidBrandingAsset(request, kind);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}
